namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Velinin gördüğü tahsilat (spec 0015): tarih, tutar, yöntem, makbuz no ve iptal durumu (not, gerekçe ve kaydeden yok).</summary>
public sealed record ChildPayment(DateOnly Date, decimal Amount, string Method, long ReceiptNo, bool Cancelled);
