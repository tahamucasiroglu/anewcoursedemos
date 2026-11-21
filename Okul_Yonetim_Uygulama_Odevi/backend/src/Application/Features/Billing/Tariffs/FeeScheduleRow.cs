namespace OkulYonetim.Application.Features.Billing.Tariffs;

/// <summary>Tarife satırı (spec 0015 gereksinim 1): kimlik, ad, tutar ve etkin mi.</summary>
public sealed record FeeScheduleRow(Guid Id, string Name, decimal Amount, bool IsActive);
