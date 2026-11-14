using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.TextModels.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.TextModels.Concrete;

/// <summary>
/// OpenAI (GPT-6.1 Sol, GPT-6 Luna) — Chat Completions, <c>max_completion_tokens</c>, json_schema strict, <c>reasoning_effort</c>.
/// Önbellek otomatik (<c>prompt_tokens_details.cached_tokens</c>).
/// </summary>
public sealed class OpenAiTextModel : OpenAiCompatibleTextModelBase
{
    /// <summary>Varsayılan API kökü.</summary>
    public static readonly Uri DefaultBaseUri = new("https://api.openai.com/v1/");

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    public OpenAiTextModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http)
        : base(model, credential, dependencies, http)
    {
    }

    /// <inheritdoc />
    protected override Uri BaseUri => DefaultBaseUri;

    /// <inheritdoc />
    protected override string MaxTokensField => "max_completion_tokens";
}
