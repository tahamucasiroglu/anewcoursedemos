namespace Konsept.Modules.Monitoring.Domain.Enums;

/// <summary>
/// Hata bildiriminin kaynağı (spec 0022 Req 11): yalnızca web, ajan ya da API; 0025'in filtresi bu üç değeri kullanır.
/// </summary>
public enum ErrorReportSource
{
    /// <summary>Web'deki "Hata bildir".</summary>
    Web = 0,

    /// <summary>Ajanın "Bir hata oldu, bildir" uyarısı.</summary>
    Agent = 1,

    /// <summary>API'nin beklenmeyen hatasından otomatik kayıt.</summary>
    Api = 2,
}
