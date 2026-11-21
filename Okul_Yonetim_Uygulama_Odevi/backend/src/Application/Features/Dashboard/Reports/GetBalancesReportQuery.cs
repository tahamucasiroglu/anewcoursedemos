using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>
/// Borç/gecikme raporunun bir sayfası (spec 0017 gereksinim 9): bugün itibarıyla geçerli planı olan öğrencilerin net, ödenen, kalan,
/// geciken ve en eski geciken vadesi; şube ve "yalnızca gecikenler" süzgeci; tarih aralığı yok; 50'lik sayfa. Yalnızca kurucu.
/// </summary>
public sealed record GetBalancesReportQuery(Guid? BranchId, bool OverdueOnly, int Page = 1) : IRequest<BalancesReportPage>, IReportFilter
{
    public DateOnly? StartDate => null;

    public DateOnly? EndDate => null;

    public Guid? ClassroomId => null;

    public int? MinAbsent => null;
}
