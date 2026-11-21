namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>Devamsızlık raporu satırı (spec 0017 gereksinim 7): öğrenci, okul no, bugünkü şube ve sınıfı, aralıktaki yok ve geç gün sayısı (TC, iletişim, sağlık yok).</summary>
public sealed record AttendanceReportRow(Guid StudentId, string FullName, string? SchoolNumber, string BranchName, string? ClassroomName, int AbsentDays, int LateDays);
