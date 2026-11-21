namespace OkulYonetim.Application.Features.Billing.Ledger;

/// <summary>Cari toplamları (spec 0015 gereksinim 8): borç (geçerli planların neti), geçerli tahsilat toplamı, kalan ve bugün geciken tutar.</summary>
public sealed record AccountTotals(decimal Net, decimal Paid, decimal Remaining, decimal Overdue)
{
    public static AccountTotals Zero { get; } = new(0m, 0m, 0m, 0m);

    public static AccountTotals Sum(IEnumerable<AccountTotals> all) =>
        all?.Aggregate(Zero, (a, b) => new AccountTotals(a.Net + b.Net, a.Paid + b.Paid, a.Remaining + b.Remaining, a.Overdue + b.Overdue)) ?? Zero;
}
