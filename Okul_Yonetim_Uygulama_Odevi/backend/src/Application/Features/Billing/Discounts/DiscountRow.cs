namespace OkulYonetim.Application.Features.Billing.Discounts;

/// <summary>İndirim/burs satırı (spec 0015 gereksinim 2): kimlik, ad, yüzde ve etkin mi.</summary>
public sealed record DiscountRow(Guid Id, string Name, int Percent, bool IsActive);
