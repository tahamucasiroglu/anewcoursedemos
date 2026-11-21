namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>Tahsilat raporunun bütün sonuç üzerinden toplamları: geçerli tahsilat tutarı ve sayısı, iptal sayısı, yönteme göre toplamlar (iptaller toplama girmez).</summary>
public sealed record PaymentsReportTotals(decimal ValidAmount, int ValidCount, int CancelledCount, IReadOnlyList<PaymentMethodTotal> ByMethod);
