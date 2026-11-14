namespace TahaKonsept.Modules.Providers.Application.Common;

/// <summary>
/// Providers modülünün hata kodları (<c>Error.Code</c>). <see cref="IsTransient"/> yeniden denenebilir hataları belirler.
/// </summary>
public static class ProviderErrorCodes
{
    /// <summary>Platform için anahtar girilmemiş.</summary>
    public const string KeyMissing = "providers.key_missing";

    /// <summary>Kayıtlı anahtar çözülemedi.</summary>
    public const string KeyCorrupt = "providers.key_corrupt";

    /// <summary>Bilinmeyen platform.</summary>
    public const string UnknownPlatform = "providers.unknown_platform";

    /// <summary>Katalogda model yok.</summary>
    public const string ModelNotFound = "providers.model_not_found";

    /// <summary>Model bu iş için uygun değil (yetenek / çalışma yeri).</summary>
    public const string ModelNotSupported = "providers.model_not_supported";

    /// <summary>Geçersiz istek (boş prompt vb.).</summary>
    public const string InvalidRequest = "providers.invalid_request";

    /// <summary>Sağlayıcı anahtarı reddetti (401/403).</summary>
    public const string Unauthorized = "providers.unauthorized";

    /// <summary>Hız sınırı (429).</summary>
    public const string RateLimited = "providers.rate_limited";

    /// <summary>Sağlayıcı geçici hatası (5xx, aşırı yük).</summary>
    public const string Transient = "providers.transient";

    /// <summary>Ağ hatası / zaman aşımı.</summary>
    public const string Network = "providers.network";

    /// <summary>Üretim çağrısı zaman aşımına uğradı (yeniden denenmez: sağlayıcı işi işleyip ücretlendirmiş olabilir).</summary>
    public const string Timeout = "providers.timeout";

    /// <summary>İş gönderildi (ücretlendi) ama sonucu alınamadı (yeniden denenmez: yeniden gönderim ikinci kez ücretlenir).</summary>
    public const string JobResultLost = "providers.job_result_lost";

    /// <summary>Sağlayıcı isteği reddetti (400 vb.).</summary>
    public const string BadRequest = "providers.bad_request";

    /// <summary>Model yanıtı reddetti (güvenlik).</summary>
    public const string Refused = "providers.refused";

    /// <summary>Çıktı token sınırına takıldı.</summary>
    public const string Truncated = "providers.truncated";

    /// <summary>JSON çıktı onarımdan sonra da geçersiz.</summary>
    public const string InvalidJson = "providers.invalid_json";

    /// <summary>Boş yanıt.</summary>
    public const string EmptyResponse = "providers.empty_response";

    /// <summary>Aylık bütçe aşıldı.</summary>
    public const string BudgetExceeded = "providers.budget_exceeded";

    /// <summary>Kayıt bulunamadı.</summary>
    public const string NotFound = "providers.not_found";

    /// <summary>Yeniden denenebilir hata mı.</summary>
    /// <param name="code">Hata kodu.</param>
    public static bool IsTransient(string code) => code is RateLimited or Transient or Network;
}
