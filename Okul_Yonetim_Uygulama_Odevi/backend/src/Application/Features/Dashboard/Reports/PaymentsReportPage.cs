namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>Tahsilat raporunun bir sayfası: satırlar, sayfa, sayfa boyu (50), toplam satır ve bütün sonucun toplamları.</summary>
public sealed record PaymentsReportPage(IReadOnlyList<PaymentReportRow> Items, int Page, int PageSize, int TotalCount, PaymentsReportTotals Totals);
