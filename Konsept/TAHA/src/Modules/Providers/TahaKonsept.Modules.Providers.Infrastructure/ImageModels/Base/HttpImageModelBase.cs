using System.Text.Json;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.ImageModels.Base;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Base;

/// <summary>
/// HTTP tabanlı online görsel modellerinin ortak tabanı: HTTP istemcisi + zamanlama ayarları, gönderme (<see cref="SendAsync"/>),
/// JSON okuma (<see cref="ReadJson{T}"/>), sonuç URL'lerini indirme (<see cref="DownloadAllAsync"/>), tek görsel üreten
/// API'lerde adet kadar tekrarlama (<see cref="RepeatAsync"/>) ve görsel başı kullanımlı çıktı (<see cref="Output"/>).
/// </summary>
public abstract class HttpImageModelBase : ImageModelBase
{
    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    /// <param name="options">Zamanlama ayarları.</param>
    protected HttpImageModelBase(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http, MediaModelOptions options)
        : base(model, credential, dependencies)
    {
        Http = http ?? throw new ArgumentNullException(nameof(http));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>HTTP istemcisi.</summary>
    protected HttpClient Http { get; }

    /// <summary>Zamanlama ayarları.</summary>
    protected MediaModelOptions Options { get; }

    /// <summary>Varsayılan MIME (sağlayıcı bildirmezse).</summary>
    protected virtual string DefaultMimeType => "image/png";

    /// <summary>Sonuç dosyası indirmede en fazla deneme (geçici hatalar).</summary>
    protected const int MaxDownloadAttempts = 3;

    /// <summary>
    /// Tek idempotent HTTP çağrısını (durum sorgusu, sonuç indirme) ağ istisnalarına karşı korur: bağlantı hatası, gövde okunurken kopma ve
    /// zaman aşımı <c>providers.network</c> sonucuna çevrilir (istisna üst katmana çıkıp tüm üretimi yeniden göndertmesin).
    /// </summary>
    /// <param name="call">Çağrı.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected async Task<Result<MediaHttpResponse>> GuardAsync(Func<Task<Result<MediaHttpResponse>>> call, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(call);
        try
        {
            return await call();
        }
        catch (HttpRequestException ex)
        {
            return ProviderHttpErrors.Network(PlatformDisplayName, ex.Message);
        }
        catch (IOException ex)
        {
            return ProviderHttpErrors.Network(PlatformDisplayName, ex.Message);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            return ProviderHttpErrors.Network(PlatformDisplayName, "zaman aşımı (" + ex.Message + ")");
        }
    }

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

    /// <summary>Sonuç URL'lerini (ya da data URI'lerini) indirir.</summary>
    /// <param name="urls">Adresler.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected async Task<Result<ImageModelOutput>> DownloadAllAsync(IReadOnlyList<string> urls, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(urls);
        if (urls.Count == 0)
        {
            return Error.Failure(ProviderErrorCodes.EmptyResponse, $"{PlatformDisplayName} görsel döndürmedi.");
        }

        var images = new List<byte[]>(urls.Count);
        string? mime = null;
        foreach (var url in urls)
        {
            // İndirme idempotent GET: geçici hatada yalnız bu dosya yeniden denenir (üretim yeniden gönderilmez).
            var file = await GuardAsync(() => MediaHttp.DownloadAsync(Http, url, PlatformDisplayName, ct), ct);
            for (var retry = 1; file.IsFailure && ProviderErrorCodes.IsTransient(file.Error.Code) && retry < MaxDownloadAttempts; retry++)
            {
                await Task.Delay(Options.PollInterval, ct);
                file = await GuardAsync(() => MediaHttp.DownloadAsync(Http, url, PlatformDisplayName, ct), ct);
            }

            if (file.IsFailure)
            {
                return file.Error;
            }

            images.Add(file.Value.Body);
            mime ??= file.Value.ContentType;
        }

        return Output(images, mime is not null && mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? mime : DefaultMimeType);
    }

    /// <summary>Tek çağrıda bir görsel veren API'lerde isteği adet kadar tekrarlar ve sonuçları birleştirir.</summary>
    /// <param name="count">Adet.</param>
    /// <param name="single">Tek görsel üretimi.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected static async Task<Result<ImageModelOutput>> RepeatAsync(
        int count,
        Func<CancellationToken, Task<Result<ImageModelOutput>>> single,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(single);
        var images = new List<byte[]>();
        var usage = CallUsage.Empty;
        var mime = string.Empty;
        for (var i = 0; i < Math.Max(1, count); i++)
        {
            var one = await single(ct);
            if (one.IsFailure)
            {
                // Önceki görseller ücretlendi: atılıp yeniden üretilmesin, eldekiler (kullanımıyla) döner; hiç yoksa hata.
                return images.Count > 0
                    ? new ImageModelOutput(images, mime, usage.Add(ProviderBilledUsage.Read(one.Error)))
                    : one.Error;
            }

            images.AddRange(one.Value.Images);
            usage = usage.Add(one.Value.Usage);
            mime = one.Value.MimeType;
        }

        return new ImageModelOutput(images, mime, usage);
    }

    /// <summary>Görsel başı kullanımlı çıktı.</summary>
    /// <param name="images">Görseller.</param>
    /// <param name="mimeType">MIME türü.</param>
    protected static ImageModelOutput Output(IReadOnlyList<byte[]> images, string mimeType) =>
        new(images, mimeType, new CallUsage { Images = images.Count });

    /// <summary>Negatif promptu destek olmayan API'ler için prompta ekler.</summary>
    /// <param name="request">İstek.</param>
    protected static string PromptWithNegative(ImageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return string.IsNullOrWhiteSpace(request.NegativePrompt)
            ? request.Prompt
            : request.Prompt + "\n\nAvoid: " + request.NegativePrompt.Trim();
    }
}
