namespace Konsept.Modules.Monitoring.Domain.Enums;

/// <summary>
/// Hata bildiriminin durumu (spec 0022 Req 11, spec 0025 Req 2): her bildirim "Yeni" başlar; admin İnceleniyor / Çözüldü / Yok sayıldı
/// yapabilir. "Açık" = Yeni + İnceleniyor. Değerler veritabanında sayı olarak saklanır; yalnızca sona eklenir.
/// </summary>
public enum ErrorReportStatus
{
    /// <summary>Yeni.</summary>
    New = 0,

    /// <summary>İnceleniyor.</summary>
    Reviewing = 1,

    /// <summary>Çözüldü.</summary>
    Resolved = 2,

    /// <summary>Yok sayıldı.</summary>
    Ignored = 3,
}
