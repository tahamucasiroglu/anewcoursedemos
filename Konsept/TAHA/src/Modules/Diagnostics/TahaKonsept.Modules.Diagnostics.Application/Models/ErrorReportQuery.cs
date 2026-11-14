using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Models;

/// <summary>
/// Hata bildirimi listesi filtresi (<c>GET api/diagnostics/error-reports?status=New&amp;page=1&amp;pageSize=50</c>). En yeni önce.
/// </summary>
public sealed record ErrorReportQuery
{
    /// <summary>Durum; <c>null</c> ise hepsi.</summary>
    public ErrorReportStatus? Status { get; init; }

    /// <summary>Sayfa (1'den başlar).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Sayfa boyutu (1–200, varsayılan 50).</summary>
    public int PageSize { get; init; } = 50;
}
