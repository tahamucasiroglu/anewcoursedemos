using System.Text.Json.Nodes;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.TextModels.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.TextModels.Concrete;

/// <summary>
/// Z.ai GLM (GLM-5.3) — OpenAI-uyumlu uç (<c>api.z.ai/api/paas/v4</c>). Efor açıkça gönderilir (sağlayıcı varsayılanı max);
/// yapılandırılmış çıktı <c>json_object</c> + şema talimatı (json_schema desteği doğrulanmadı); web araması yerleşik araçla.
/// </summary>
public sealed class GlmTextModel : OpenAiCompatibleTextModelBase
{
    /// <summary>Varsayılan API kökü.</summary>
    public static readonly Uri DefaultBaseUri = new("https://api.z.ai/api/paas/v4/");

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    public GlmTextModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http)
        : base(model, credential, dependencies, http)
    {
    }

    /// <inheritdoc />
    protected override Uri BaseUri => DefaultBaseUri;

    /// <inheritdoc />
    protected override bool SupportsJsonSchema => false;

    /// <inheritdoc />
    protected override void ApplyEffort(JsonObject body, EffortLevel? effort) =>
        base.ApplyEffort(body, effort ?? Model.DefaultEffort ?? EffortLevel.High);

    /// <inheritdoc />
    protected override void ApplyWebSearch(JsonObject body)
    {
        ArgumentNullException.ThrowIfNull(body);
        body["tools"] = new JsonArray(new JsonObject
        {
            ["type"] = "web_search",
            ["web_search"] = new JsonObject { ["enable"] = true },
        });
    }
}
