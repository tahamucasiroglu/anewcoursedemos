using OkulYonetim.Application.Features.Organization;

namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Cari listesinin sayfası (spec 0015): 25 satır, toplam satır sayısı, süzülmüş bütün satırların toplamları ve süzgeç için kişinin tahsilat şubeleri.</summary>
public sealed record StudentAccountPage(IReadOnlyList<StudentAccountRow> Items, int Page, int PageSize, int TotalCount, AccountTotals Totals, IReadOnlyList<BranchSummary> Branches);
