using Microsoft.Extensions.Logging;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Base;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.TextModels.Abstract;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.TextModels.Concrete;

/// <summary>
/// <see cref="TextModelFactoryBase"/> uygulaması: platforma göre somut metin modelini oluşturur. Yeni sağlayıcı = yeni somut
/// sınıf + buradaki eşlemeye bir satır. HTTP istemcileri <see cref="HttpClientName"/> adlı havuzdan (uzun zaman aşımı) gelir.
/// </summary>
public sealed class TextModelFactory : TextModelFactoryBase
{
    /// <summary>Model çağrıları için adlandırılmış HttpClient.</summary>
    public const string HttpClientName = "providers-models";

    private readonly IHttpClientFactory _httpClients;
    private readonly ICallRecorder _recorder;
    private readonly ILoggerFactory _loggers;

    /// <summary>Kurucu.</summary>
    /// <param name="httpClients">HttpClient fabrikası.</param>
    /// <param name="recorder">Çağrı kaydedici.</param>
    /// <param name="loggers">Logger fabrikası.</param>
    public TextModelFactory(IHttpClientFactory httpClients, ICallRecorder recorder, ILoggerFactory loggers)
    {
        _httpClients = httpClients;
        _recorder = recorder;
        _loggers = loggers;
    }

    /// <inheritdoc />
    protected override ITextModel? CreateCore(ModelInfo model, ProviderCredential credential)
    {
        var dependencies = new AiModelDependencies(_recorder, _loggers.CreateLogger("TahaKonsept.Providers.TextModels." + model.Platform));
        var http = _httpClients.CreateClient(HttpClientName);
        return model.Platform switch
        {
            ProviderPlatforms.Anthropic => new AnthropicTextModel(model, credential, dependencies, http),
            ProviderPlatforms.OpenAi => new OpenAiTextModel(model, credential, dependencies, http),
            ProviderPlatforms.OpenRouter => new OpenRouterTextModel(model, credential, dependencies, http),
            ProviderPlatforms.Zai => new GlmTextModel(model, credential, dependencies, http),
            ProviderPlatforms.Moonshot => new KimiTextModel(model, credential, dependencies, http),
            ProviderPlatforms.LocalOpenAi => new LocalOpenAiTextModel(model, credential, dependencies, http),
            _ => null,
        };
    }
}
