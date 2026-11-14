using System.Text.Json.Nodes;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.TextModels.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.TextModels.Concrete;

/// <summary>
/// OpenRouter — geçiş model kimliği (<c>openrouter:&lt;sağlayıcı/model&gt;</c>), maliyet yanıtta (<c>usage: {include: true}</c> →
/// <c>usage.cost</c>), efor <c>reasoning.effort</c>, web araması <c>plugins: [{id: "web"}]</c>.
/// </summary>
public sealed class OpenRouterTextModel : OpenAiCompatibleTextModelBase
{
    /// <summary>Varsayılan API kökü.</summary>
    public static readonly Uri DefaultBaseUri = new("https://openrouter.ai/api/v1/");

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    public OpenRouterTextModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http)
        : base(model, credential, dependencies, http)
    {
    }

    /// <inheritdoc />
    protected override Uri BaseUri => DefaultBaseUri;

    /// <inheritdoc />
    protected override JsonObject BuildRequestBody(TextRequest request)
    {
        var body = base.BuildRequestBody(request);
        body["usage"] = new JsonObject { ["include"] = true };
        return body;
    }

    /// <inheritdoc />
    protected override void ApplyEffort(JsonObject body, EffortLevel? effort)
    {
        ArgumentNullException.ThrowIfNull(body);
        var value = effort ?? Model.DefaultEffort;
        if (value is { } e)
        {
            body["reasoning"] = new JsonObject { ["effort"] = e is EffortLevel.XHigh or EffortLevel.Max ? "high" : MapEffort(e) };
        }
    }

    /// <inheritdoc />
    protected override void ApplyWebSearch(JsonObject body)
    {
        ArgumentNullException.ThrowIfNull(body);
        body["plugins"] = new JsonArray(new JsonObject { ["id"] = "web" });
    }
}
