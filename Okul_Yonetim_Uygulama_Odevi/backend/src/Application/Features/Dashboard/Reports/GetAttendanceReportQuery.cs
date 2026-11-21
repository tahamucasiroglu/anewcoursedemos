using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>
/// Devamsızlık raporunun bir sayfası (spec 0017 gereksinim 7): aralıkta alınmış yoklamalardan öğrenci başına yok ve geç gün sayısı; şube,
/// sınıf ve "en az N gün yok" süzgeci; 50'lik sayfa, toplamlar bütün sonuç üzerinden. Yalnızca kurucu (<c>DashboardGuard</c>).
/// </summary>
public sealed record GetAttendanceReportQuery(DateOnly? StartDate, DateOnly? EndDate, Guid? BranchId, Guid? ClassroomId, int? MinAbsent, int Page = 1)
    : IRequest<AttendanceReportPage>, IReportFilter
{
    public bool OverdueOnly => false;
}
