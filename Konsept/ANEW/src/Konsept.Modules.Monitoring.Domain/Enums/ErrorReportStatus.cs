namespace Konsept.Modules.Monitoring.Domain.Enums;

/// <summary>
/// Hata bildiriminin durumu (spec 0022 Req 11): her bildirim "Yeni" başlar; diğer durumlar ve yönetimi 0025'tedir.
/// </summary>
public enum ErrorReportStatus
{
    /// <summary>Yeni.</summary>
    New = 0,
}
