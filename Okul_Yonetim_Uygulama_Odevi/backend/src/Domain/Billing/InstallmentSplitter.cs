namespace OkulYonetim.Domain.Billing;

/// <summary>
/// Taksit bölmesi (spec 0015 gereksinim 3, BR-7; saf hesap): net tutar <see cref="MinCount"/>–<see cref="MaxCount"/> eşit taksite
/// kuruşa aşağı yuvarlanarak bölünür, kuruş farkı son taksite eklenir — toplam her zaman neti tutar. Vadeler ilk vadeden itibaren
/// aylık ve aynı gün; ayda o gün yoksa ayın son günü (her vade ilk vadeden hesaplanır: 31 Ocak → 28/29 Şubat → 31 Mart).
/// </summary>
public static class InstallmentSplitter
{
    public const int MinCount = 1;
    public const int MaxCount = 12;

    public static bool IsValidCount(int count) => count is >= MinCount and <= MaxCount;

    public static IReadOnlyList<InstallmentLine> Split(decimal net, int count, DateOnly firstDue)
    {
        if (!MoneyRules.IsPositiveMoney(net))
        {
            throw new ArgumentOutOfRangeException(nameof(net), "Net tutar sıfırdan büyük ve en fazla iki ondalıklı olmalı.");
        }

        if (!IsValidCount(count))
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Taksit sayısı 1–12 olmalı.");
        }

        decimal each = decimal.Round(net / count, 2, MidpointRounding.ToZero);
        decimal last = net - (each * (count - 1));
        List<InstallmentLine> lines = new(count);
        for (int i = 0; i < count; i++)
        {
            lines.Add(new InstallmentLine(i + 1, firstDue.AddMonths(i), i == count - 1 ? last : each));
        }

        return lines;
    }
}
