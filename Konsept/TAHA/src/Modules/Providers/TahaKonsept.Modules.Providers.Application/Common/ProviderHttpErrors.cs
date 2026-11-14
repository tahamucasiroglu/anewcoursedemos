using System.Net;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Modules.Providers.Application.Common;

/// <summary>
/// Sağlayıcının HTTP durum kodunu Türkçe mesajlı <see cref="Error"/>'a çevirir (tüm platformlarda aynı eşleme).
/// 401/403 → anahtar geçersiz, 429 → hız sınırı, 5xx/529 → geçici, diğer 4xx → istek reddedildi.
/// </summary>
public static class ProviderHttpErrors
{
    /// <summary>Gövdeden alınan ayrıntının en fazla uzunluğu.</summary>
    private const int MaxDetail = 400;

    /// <summary>HTTP hatasını eşler.</summary>
    /// <param name="platformName">Görünen platform adı.</param>
    /// <param name="statusCode">HTTP durum kodu.</param>
    /// <param name="detail">Sağlayıcının hata metni (kısaltılır).</param>
    public static Error FromStatus(string platformName, int statusCode, string? detail)
    {
        var info = string.IsNullOrWhiteSpace(detail) ? string.Empty : " Ayrıntı: " + Trim(detail);
        return statusCode switch
        {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden =>
                Error.Failure(ProviderErrorCodes.Unauthorized, $"{platformName} API anahtarı reddedildi (HTTP {statusCode}). Ayarlar → API anahtarları bölümünden kontrol edin.{info}"),
            (int)HttpStatusCode.TooManyRequests =>
                Error.Failure(ProviderErrorCodes.RateLimited, $"{platformName} hız sınırına takıldı (HTTP 429).{info}"),
            >= 500 =>
                Error.Failure(ProviderErrorCodes.Transient, $"{platformName} geçici olarak yanıt veremiyor (HTTP {statusCode}).{info}"),
            _ => Error.Failure(ProviderErrorCodes.BadRequest, $"{platformName} isteği reddetti (HTTP {statusCode}).{info}"),
        };
    }

    /// <summary>Ağ hatası.</summary>
    /// <param name="platformName">Görünen platform adı.</param>
    /// <param name="message">Ayrıntı.</param>
    public static Error Network(string platformName, string message) =>
        Error.Failure(ProviderErrorCodes.Network, $"{platformName} sunucusuna ulaşılamadı: {Trim(message)}");

    /// <summary>Zaman aşımı (yeniden denenmez).</summary>
    /// <param name="platformName">Görünen platform adı.</param>
    /// <param name="message">Ayrıntı.</param>
    public static Error Timeout(string platformName, string message) =>
        Error.Failure(ProviderErrorCodes.Timeout, $"{platformName} yanıtı zaman aşımına uğradı: {Trim(message)}");

    /// <summary>Uzun metni kısaltır.</summary>
    /// <param name="text">Metin.</param>
    private static string Trim(string text) => text.Length <= MaxDetail ? text : text[..MaxDetail] + "…";
}
