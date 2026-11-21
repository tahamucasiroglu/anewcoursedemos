namespace OkulYonetim.Domain.Billing;

/// <summary>
/// Para kuralları (spec 0015 C-3, BR-7; <c>docs/conventions.md</c> Data rules 1): Türk lirası, <c>decimal</c>, en fazla iki ondalık
/// (kuruş); <c>float</c>/<c>double</c> yok. Tarife tutarı en fazla <see cref="MaxFeeAmount"/>. Yüzde hesabı kuruşa yarım yukarı
/// (<see cref="MidpointRounding.AwayFromZero"/>), taksit bölmesi kuruşa aşağı yuvarlanır (<see cref="InstallmentSplitter"/>).
/// </summary>
public static class MoneyRules
{
    public const decimal MaxFeeAmount = 10_000_000.00m;

    /// <summary>Tutarın en fazla iki ondalığı var mı (kuruştan küçük parça yok).</summary>
    public static bool HasAtMostTwoDecimals(decimal amount) => decimal.Round(amount, 2) == amount;

    /// <summary>Sıfırdan büyük ve en fazla iki ondalıklı tutar.</summary>
    public static bool IsPositiveMoney(decimal amount) => amount > 0m && HasAtMostTwoDecimals(amount);

    /// <summary>İndirim tutarı: tarife × yüzde / 100, kuruşa yarım yukarı.</summary>
    public static decimal DiscountOf(decimal feeAmount, int percent) =>
        decimal.Round(feeAmount * percent / 100m, 2, MidpointRounding.AwayFromZero);
}
