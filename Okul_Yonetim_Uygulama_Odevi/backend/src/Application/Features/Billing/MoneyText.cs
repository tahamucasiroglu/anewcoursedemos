using System.Globalization;

namespace OkulYonetim.Application.Features.Billing;

/// <summary>
/// Para metni (spec 0015 Accessibility, C-3): gösterim "1.234,50" (binlik nokta, ondalık virgül, her zaman iki ondalık) — kültür
/// ayarına (T1, sunucunun yerel ayarı) bağlı değil, sabit biçimle. İstekteki tutar metni (JSON sayısının ham metni) değişmez kültürle
/// ve yalnızca işaret + ondalık noktayla ayrıştırılır (üs, binlik ayırıcı, boşluk yok); geçersiz metin alan hatasına düşer.
/// </summary>
public static class MoneyText
{
    private static readonly NumberFormatInfo Turkish = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberGroupSizes = [3],
        NegativeSign = "-",
    };

    public static string Format(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero).ToString("N2", Turkish);

    /// <summary>Gün metni "15.12.2026".</summary>
    public static string Day(DateOnly day) => day.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    public static bool TryParseAmount(string? text, out decimal amount)
    {
        amount = 0m;
        return !string.IsNullOrWhiteSpace(text)
            && decimal.TryParse(text.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount);
    }

    public static bool TryParsePercent(string? text, out int percent)
    {
        percent = 0;
        return !string.IsNullOrWhiteSpace(text)
            && int.TryParse(text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out percent);
    }
}
