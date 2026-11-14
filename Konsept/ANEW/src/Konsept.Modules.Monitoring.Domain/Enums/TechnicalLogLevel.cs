namespace Konsept.Modules.Monitoring.Domain.Enums;

/// <summary>Teknik log seviyesi (spec 0008 Req 1; sayısal değerler <c>Microsoft.Extensions.Logging.LogLevel</c> ile aynı).</summary>
public enum TechnicalLogLevel
{
    /// <summary>Bilgi (veritabanına yazılmaz, Req 6).</summary>
    Information = 2,

    /// <summary>Uyarı.</summary>
    Warning = 3,

    /// <summary>Hata.</summary>
    Error = 4,

    /// <summary>Kritik.</summary>
    Critical = 5,
}
