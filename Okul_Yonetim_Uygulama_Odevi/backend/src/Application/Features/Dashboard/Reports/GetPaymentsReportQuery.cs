using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>
/// Tahsilat raporunun bir sayfası (spec 0017 gereksinim 8): tahsilat günü aralıkta olan bütün tahsilatlar (iptaller "İptal" işaretli),
/// şube süzgeci, 50'lik sayfa; geçerli toplam ve yönteme göre toplamlar bütün sonuç üzerinden. Yalnızca kurucu.
/// </summary>
public sealed record GetPaymentsReportQuery(DateOnly? StartDate, DateOnly? EndDate, Guid? BranchId, int Page = 1) : IRequest<PaymentsReportPage>, IReportFilter
{
    public Guid? ClassroomId => null;

    public int? MinAbsent => null;

    public bool OverdueOnly => false;
}
