using System.Text.Json.Nodes;
using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Engines.Image.ComfyUi.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Image.ComfyUi.Base;

/// <summary>
/// Şablon doldurma tabanı: şablonu derin kopyalar, tüm string değerleri gezer; değerin tamamı
/// <c>{{width}}</c>/<c>{{height}}</c>/<c>{{seed}}</c>/<c>{{steps}}</c> ise sayıya çevirir, metin içindeki
/// <c>{{prompt}}</c>/<c>{{negative}}</c>/<c>{{filenamePrefix}}</c> yer tutucularını değiştirir.
/// Alt sınıf yalnızca <see cref="LoadTemplates"/> sağlar.
/// </summary>
public abstract class ComfyUiWorkflowProviderBase : IComfyUiWorkflowProvider
{
    private readonly Lazy<IReadOnlyDictionary<string, ComfyUiWorkflowTemplate>> _templates;

    /// <summary>Tembel yükleyiciyi hazırlar.</summary>
    protected ComfyUiWorkflowProviderBase()
    {
        _templates = new Lazy<IReadOnlyDictionary<string, ComfyUiWorkflowTemplate>>(
            () => LoadTemplates().ToDictionary(t => t.ModelKey, StringComparer.OrdinalIgnoreCase),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ModelKeys => _templates.Value.Keys.Order(StringComparer.Ordinal).ToList();

    /// <inheritdoc />
    public ComfyUiWorkflowTemplate? GetTemplate(string modelKey) => _templates.Value.GetValueOrDefault(modelKey);

    /// <inheritdoc />
    public Result<JsonObject> Build(ImageRequest request, string filenamePrefix)
    {
        var template = GetTemplate(request.ModelKey);
        if (template is null)
        {
            return Error.Validation("comfyui.workflow.missing", $"'{request.ModelKey}' için ComfyUI iş akışı şablonu yok.");
        }

        var numbers = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["{{width}}"] = request.Width,
            ["{{height}}"] = request.Height,
            ["{{seed}}"] = request.Seed >= 0 ? request.Seed : Random.Shared.NextInt64(0, long.MaxValue),
            ["{{steps}}"] = request.Steps ?? template.DefaultSteps,
        };
        var texts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{prompt}}"] = request.Prompt.Trim(),
            ["{{negative}}"] = request.NegativePrompt?.Trim() ?? string.Empty,
            ["{{filenamePrefix}}"] = filenamePrefix,
        };

        var workflow = (JsonObject)template.Workflow.DeepClone();
        Substitute(workflow, numbers, texts);
        return workflow;
    }

    /// <summary>Şablonları yükler.</summary>
    /// <returns>Şablonlar.</returns>
    protected abstract IEnumerable<ComfyUiWorkflowTemplate> LoadTemplates();

    private static void Substitute(JsonNode node, IReadOnlyDictionary<string, long> numbers, IReadOnlyDictionary<string, string> texts)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    var current = obj[key];
                    var replaced = Replace(current, numbers, texts);
                    if (!ReferenceEquals(current, replaced))
                    {
                        obj[key] = replaced;
                    }
                }

                break;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    var current = array[i];
                    var replaced = Replace(current, numbers, texts);
                    if (!ReferenceEquals(current, replaced))
                    {
                        array[i] = replaced;
                    }
                }

                break;
        }
    }

    private static JsonNode? Replace(JsonNode? node, IReadOnlyDictionary<string, long> numbers, IReadOnlyDictionary<string, string> texts)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var text))
        {
            if (numbers.TryGetValue(text, out var number))
            {
                return JsonValue.Create(number);
            }

            if (!text.Contains("{{", StringComparison.Ordinal))
            {
                return node;
            }

            foreach (var (placeholder, replacement) in texts)
            {
                text = text.Replace(placeholder, replacement, StringComparison.Ordinal);
            }

            return JsonValue.Create(text);
        }

        if (node is not null)
        {
            Substitute(node, numbers, texts);
        }

        return node;
    }
}
