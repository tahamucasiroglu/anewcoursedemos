namespace Konsept.Modules.AiGateway.Application.Enums;

/// <summary>Sağlayıcı istemcisinin tek bir denemeden döndürdüğü sonuç türü (spec 0013 Req 7–8).</summary>
public enum ProviderAttemptKind
{
    /// <summary>Sağlayıcı başarılı yanıt verdi (tam ya da yarım).</summary>
    Completed,

    /// <summary>Sağlayıcı hata durum koduyla yanıt verdi.</summary>
    HttpError,

    /// <summary>Bağlantı hiç kurulamadı; istek sağlayıcıya ulaşmadı (tekrar edilebilir, kayıt yok).</summary>
    ConnectionFailed,

    /// <summary>İstek gönderildi ama yanıt alınamadı (bağlantı yanıt sırasında koptu); tekrar edilmez.</summary>
    NoResponse,
}
