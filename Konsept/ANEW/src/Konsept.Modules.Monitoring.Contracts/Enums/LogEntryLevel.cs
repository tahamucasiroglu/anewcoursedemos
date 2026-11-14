namespace Konsept.Modules.Monitoring.Contracts.Enums;

/// <summary>Teknik log seviyesi (sayısal değerler <c>Microsoft.Extensions.Logging.LogLevel</c> ile aynı).</summary>
public enum LogEntryLevel
{
    /// <summary>Bilgi.</summary>
    Information = 2,

    /// <summary>Uyarı.</summary>
    Warning = 3,

    /// <summary>Hata.</summary>
    Error = 4,

    /// <summary>Kritik.</summary>
    Critical = 5,
}
