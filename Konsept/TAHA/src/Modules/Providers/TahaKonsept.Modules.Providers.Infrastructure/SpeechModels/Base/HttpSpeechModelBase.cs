using System.Text.Json;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.SpeechModels.Base;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Base;

/// <summary>
/// HTTP tabanlı online ses modellerinin ortak tabanı: gönderme (<see cref="SendAsync"/>), JSON okuma (<see cref="ReadJson{T}"/>),
/// ses kimliği zorunluluğu (<see cref="RequireVoice"/>), biçim seçimi (<see cref="FormatOf"/>: mp3 / wav) ve çıktıyı kurma
/// (<see cref="Output"/>: süreyi dosyadan ölçer, kullanımı karakter + saniye (+ varsa token) olarak yazar; katalog fiyatı hangisi
/// tanımlıysa ona göre ücretlenir: bin karakter, dakika ya da token).
/// </summary>
public abstract class HttpSpeechModelBase : SpeechModelBase
{
    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    protected HttpSpeechModelBase(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http)
        : base(model, credential, dependencies)
    {
        Http = http ?? throw new ArgumentNullException(nameof(http));
    }

    /// <summary>HTTP istemcisi.</summary>
    protected HttpClient Http { get; }

    /// <summary>İsteği gönderir (HTTP hataları eşlenir).</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected Task<Result<MediaHttpResponse>> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
        MediaHttp.SendAsync(Http, request, PlatformDisplayName, ct);

    /// <summary>Yanıtı JSON olarak okur.</summary>
    /// <typeparam name="T">Değer.</typeparam>
    /// <param name="response">Yanıt.</param>
    /// <param name="read">Okuyucu.</param>
    protected Result<T> ReadJson<T>(MediaHttpResponse response, Func<JsonElement, Result<T>> read) =>
        MediaJson.Read(response, PlatformDisplayName, read);

    /// <summary>Ses kimliğini zorunlu kılar.</summary>
    /// <param name="request">İstek.</param>
    /// <returns>Ses kimliği ya da <c>providers.invalid_request</c>.</returns>
    protected Result<string> RequireVoice(SpeechRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return string.IsNullOrWhiteSpace(request.Voice)
            ? Error.Validation(ProviderErrorCodes.InvalidRequest, $"{DisplayName} için ses seçilmeli (api/providers/voices).")
            : request.Voice.Trim();
    }

    /// <summary>İstenen biçimi mp3 / wav'a indirger.</summary>
    /// <param name="request">İstek.</param>
    protected static string FormatOf(SpeechRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return AudioFormats.NormalizeFormat(request.Format);
    }

    /// <summary>Çıktıyı kurar: süreyi ölçer, kullanımı yazar.</summary>
    /// <param name="audio">Ses dosyası.</param>
    /// <param name="mimeType">MIME türü.</param>
    /// <param name="request">İstek (karakter sayısı için).</param>
    /// <param name="tokens">Sağlayıcının bildirdiği token kullanımı (yoksa null).</param>
    protected static Result<SpeechModelOutput> Output(byte[] audio, string mimeType, SpeechRequest request, CallUsage? tokens = null)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(request);
        if (audio.Length == 0)
        {
            return Error.Failure(ProviderErrorCodes.EmptyResponse, "Sağlayıcı boş ses döndürdü.");
        }

        var seconds = AudioFormats.DurationSeconds(audio);
        var usage = (tokens ?? CallUsage.Empty) with
        {
            Characters = request.Text.Length,
            AudioSeconds = seconds ?? 0m,
        };
        return new SpeechModelOutput(audio, mimeType, seconds, usage);
    }
}
