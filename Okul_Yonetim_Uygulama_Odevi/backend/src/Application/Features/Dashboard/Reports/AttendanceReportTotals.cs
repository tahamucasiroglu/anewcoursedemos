namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>Devamsızlık raporunun bütün sonuç üzerinden toplamları: yok ve geç gün sayıları.</summary>
public sealed record AttendanceReportTotals(int AbsentDays, int LateDays);
