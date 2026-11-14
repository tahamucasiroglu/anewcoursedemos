namespace TahaKonsept.Shared.Contracts.Diagnostics;

/// <summary>
/// Hata bildirimi durum değişikliği: <c>PUT api/diagnostics/error-reports/{id}/status</c> → <c>{ status: "Seen" }</c>.
/// </summary>
public sealed record ErrorReportStatusUpdateRequest
{
    /// <summary>Yeni durum.</summary>
    public ErrorReportStatus Status { get; init; }
}
