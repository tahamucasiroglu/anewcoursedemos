namespace Konsept.Modules.AgentGateway.Contracts.Dtos;

/// <summary>Ajan hata bildiriminin sonucu.</summary>
public enum AgentErrorReportResult
{
    /// <summary>Kaydedildi.</summary>
    Sent = 0,

    /// <summary>Saatlik sınır aşıldı; kaydedilmedi (ajan bekletmeye almaz).</summary>
    RateLimited = 1,

    /// <summary>Bildirim başka bir hesapta oluşturuldu; kaydedilmedi.</summary>
    OtherAccount = 2,

    /// <summary>Eksik içerik; kaydedilmedi.</summary>
    Invalid = 3,
}
