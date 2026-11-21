namespace OkulYonetim.Application.Features.Billing.Payments;

/// <summary>Makbuzdaki dağılım satırı (spec 0015 gereksinim 7): taksit sırası, vadesi ve bu tahsilattan düşen tutar.</summary>
public sealed record ReceiptAllocation(int Sequence, DateOnly DueDate, decimal Amount);
