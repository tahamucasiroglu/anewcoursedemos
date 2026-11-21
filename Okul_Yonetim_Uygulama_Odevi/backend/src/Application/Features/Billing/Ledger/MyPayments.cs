namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Velinin ödeme görünümü (spec 0015 gereksinim 10): çocukların toplam kalanı ve çocuk başına cari.</summary>
public sealed record MyPayments(decimal TotalRemaining, IReadOnlyList<ChildAccount> Children);
