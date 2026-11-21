namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>Devamsızlık raporunun bir sayfası: satırlar, sayfa, sayfa boyu (50), toplam satır ve bütün sonucun toplamları.</summary>
public sealed record AttendanceReportPage(IReadOnlyList<AttendanceReportRow> Items, int Page, int PageSize, int TotalCount, AttendanceReportTotals Totals);
