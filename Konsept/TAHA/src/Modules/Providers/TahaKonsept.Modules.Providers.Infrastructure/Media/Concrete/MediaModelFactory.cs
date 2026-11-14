using Microsoft.Extensions.Logging;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.ImageModels.Abstract;
using TahaKonsept.Modules.Providers.Application.Media.Base;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.SpeechModels.Abstract;
using TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Concrete;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Concrete;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.Media.Concrete;

/// <summary>
/// <see cref="MediaModelFactoryBase"/> uygulaması: platforma göre somut görsel/ses modelini oluşturur. Yeni sağlayıcı = yeni somut
/// sınıf + buradaki eşlemeye bir satır. HTTP istemcileri <see cref="HttpClientName"/> adlı havuzdan gelir.
/// </summary>
public sealed class MediaModelFactory : MediaModelFactoryBase
{
    /// <summary>Görsel/ses çağrıları için adlandırılmış HttpClient.</summary>
    public const string HttpClientName = "providers-media";

    private readonly IHttpClientFactory _httpClients;
    private readonly ICallRecorder _recorder;
    private readonly ILoggerFactory _loggers;

    /// <summary>Kurucu.</summary>
    /// <param name="httpClients">HttpClient fabrikası.</param>
    /// <param name="recorder">Çağrı kaydedici.</param>
    /// <param name="loggers">Logger fabrikası.</param>
    public MediaModelFactory(IHttpClientFactory httpClients, ICallRecorder recorder, ILoggerFactory loggers)
    {
        _httpClients = httpClients;
        _recorder = recorder;
        _loggers = loggers;
    }

    /// <inheritdoc />
    protected override IImageModel? CreateImageCore(ModelInfo model, ProviderCredential credential)
    {
        var dependencies = Dependencies("ImageModels", model);
        var http = _httpClients.CreateClient(HttpClientName);
        var options = MediaModelOptions.Default;
        return model.Platform switch
        {
            ProviderPlatforms.Google => new GeminiImageModel(model, credential, dependencies, http, options),
            ProviderPlatforms.OpenAi => new OpenAiImageModel(model, credential, dependencies, http, options),
            ProviderPlatforms.Fal => new FalImageModel(model, credential, dependencies, http, options),
            ProviderPlatforms.Bfl => new BflImageModel(model, credential, dependencies, http, options),
            ProviderPlatforms.Zai => new ZaiImageModel(model, credential, dependencies, http, options),
            ProviderPlatforms.Replicate => new ReplicateImageModel(model, credential, dependencies, http, options),
            _ => null,
        };
    }

    /// <inheritdoc />
    protected override ISpeechModel? CreateSpeechCore(ModelInfo model, ProviderCredential credential)
    {
        var dependencies = Dependencies("SpeechModels", model);
        var http = _httpClients.CreateClient(HttpClientName);
        return model.Platform switch
        {
            ProviderPlatforms.OpenAi => new OpenAiSpeechModel(model, credential, dependencies, http),
            ProviderPlatforms.ElevenLabs => new ElevenLabsSpeechModel(model, credential, dependencies, http),
            ProviderPlatforms.Google => new GeminiSpeechModel(model, credential, dependencies, http),
            ProviderPlatforms.AzureSpeech => new AzureSpeechModel(model, credential, dependencies, http),
            ProviderPlatforms.Cartesia => new CartesiaSpeechModel(model, credential, dependencies, http),
            _ => null,
        };
    }

    /// <summary>Model bağımlılıkları.</summary>
    /// <param name="branch">Log kategorisi dalı.</param>
    /// <param name="model">Katalog kaydı.</param>
    private AiModelDependencies Dependencies(string branch, ModelInfo model) =>
        new(_recorder, _loggers.CreateLogger($"TahaKonsept.Providers.{branch}.{model.Platform}"));
}
