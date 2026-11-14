using System.Text.Json.Nodes;
using TahaKonsept.Agent.Engines.Image.ComfyUi.Base;

namespace TahaKonsept.Agent.Engines.Image.ComfyUi.Concrete;

/// <summary>Derlemeye gömülü <c>ComfyUi/Workflows/*.json</c> şablonlarını (z-image-turbo, flux2-klein-4b) yükler.</summary>
public sealed class EmbeddedComfyUiWorkflowProvider : ComfyUiWorkflowProviderBase
{
    /// <summary>Gömülü şablon kaynak adlarının öneki.</summary>
    public const string ResourcePrefix = "TahaKonsept.Agent.Engines.Image.ComfyUi.Workflows.";

    /// <inheritdoc />
    protected override IEnumerable<ComfyUiWorkflowTemplate> LoadTemplates()
    {
        var assembly = typeof(EmbeddedComfyUiWorkflowProvider).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"Gömülü kaynak açılamadı: {name}");
            var root = JsonNode.Parse(stream)?.AsObject()
                ?? throw new InvalidOperationException($"ComfyUI şablonu okunamadı: {name}");

            yield return new ComfyUiWorkflowTemplate(
                root["modelKey"]!.GetValue<string>(),
                root["defaultSteps"]!.GetValue<int>(),
                root["requiredFiles"]?.AsArray().Select(n => n!.GetValue<string>()).ToList() ?? [],
                root["workflow"]!.AsObject());
        }
    }
}
