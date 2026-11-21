namespace OkulYonetim.Application.Features.Billing.Payments;

/// <summary>Kaydedilen tahsilat (spec 0015 gereksinim 5): kimlik ve okulun sıradaki makbuz numarası.</summary>
public sealed record PaymentRecorded(Guid Id, long ReceiptNo);
