using OkulYonetim.Application.Features.Billing.Ledger;

namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>Borç/gecikme raporunun bir sayfası: satırlar, sayfa, sayfa boyu (50), toplam satır, bütün sonucun cari toplamları ve raporun günü (Türkiye).</summary>
public sealed record BalancesReportPage(IReadOnlyList<BalanceReportRow> Items, int Page, int PageSize, int TotalCount, AccountTotals Totals, DateOnly Today);
