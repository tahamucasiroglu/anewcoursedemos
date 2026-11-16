namespace Konsept.Modules.AiGateway.Domain.Enums;

/// <summary>Sağlayıcıya ulaşan bir denemenin sonucu (spec 0013 Req 11; harcama kaydında saklanır).</summary>
public enum CallOutcome
{
    /// <summary>Tam yanıt alındı.</summary>
    Success,

    /// <summary>Çıktı token sınırına takılıp yarım kaldı (Req 5).</summary>
    Truncated,

    /// <summary>Sağlayıcı hata yanıtı döndürdü (429, 5xx, diğer 4xx).</summary>
    ProviderError,

    /// <summary>İstek gönderildi, zaman aşımı içinde yanıt alınamadı (Req 8).</summary>
    NoResponse,
}
