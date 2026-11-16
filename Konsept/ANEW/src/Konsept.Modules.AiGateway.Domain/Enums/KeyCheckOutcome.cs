namespace Konsept.Modules.AiGateway.Domain.Enums;

/// <summary>Sağlayıcının maliyetsiz ucunda key doğrulamasının sonucu (spec 0004 Req 10).</summary>
public enum KeyCheckOutcome
{
    /// <summary>Sağlayıcı key'i kabul etti (2xx) → "Doğrulandı".</summary>
    Accepted,

    /// <summary>Sağlayıcı key'i reddetti (401) → key kaydedilmez.</summary>
    Rejected,

    /// <summary>Sonuçlanmadı (zaman aşımı, 429, 5xx, bağlantı hatası) ya da maliyetsiz yol yok → "Doğrulanmadı".</summary>
    Inconclusive,
}
