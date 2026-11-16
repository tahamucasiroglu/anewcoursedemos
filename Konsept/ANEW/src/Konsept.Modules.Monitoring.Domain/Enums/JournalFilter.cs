namespace Konsept.Modules.Monitoring.Domain.Enums;

/// <summary>
/// Günlük geçmişindeki seviye filtresi (spec 0021 Req 5): Tümü, Uyarı, Hata ("Hata" Hata ve Kritik kayıtları gösterir).
/// </summary>
public enum JournalFilter
{
    /// <summary>Tüm seviyeler.</summary>
    All = 0,

    /// <summary>Yalnızca Uyarı.</summary>
    Warning = 1,

    /// <summary>Hata ve Kritik.</summary>
    Error = 2,
}
