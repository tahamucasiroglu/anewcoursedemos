namespace Konsept.Modules.Monitoring.Domain.Enums;

/// <summary>
/// Olayın seviyesi (domain karşılığı; sayısal değerler Contracts ile aynıdır).
/// </summary>
public enum EventLevel
{
    /// <summary>Bilgi.</summary>
    Info = 0,

    /// <summary>Uyarı.</summary>
    Warning = 1,

    /// <summary>Hata.</summary>
    Error = 2,

    /// <summary>Kritik.</summary>
    Critical = 3,
}
