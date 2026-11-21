namespace OkulYonetim.Application.Features.Dashboard.Reports;

/// <summary>Bir ödeme yönteminin geçerli tahsilat toplamı ve sayısı (iptaller dışarıda).</summary>
public sealed record PaymentMethodTotal(string Method, decimal Amount, int Count);
