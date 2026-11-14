using System.Globalization;
using System.Text.RegularExpressions;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Base;
using TahaKonsept.Modules.Production.Normalization.Models;
using TahaKonsept.Modules.Production.Normalization.Text;

namespace TahaKonsept.Modules.Production.Normalization.Concrete.Rules;

/// <summary>
/// 4. adım — rakamlı sıra sayıları: "3. sırada" → "üçüncü sırada", "21. yüzyıl" → "yirmi birinci yüzyıl",
/// "1. Dünya Savaşı" → "Birinci Dünya Savaşı". Noktadan sonra boşluk + harf gelmelidir; ardından küçük harf geliyorsa her
/// zaman, büyük harf geliyorsa yalnız ≤ <see cref="MaxBeforeCapital"/> için sıra sayısı sayılır ("Yıl 1453. Sonra" cümle sonudur).
/// Ondalık (3.5), binlik (1.250.000) ve tarihler (önceki adım) etkilenmez.
/// </summary>
public sealed partial class OrdinalRule : NormalizationRuleBase
{
    /// <summary>Büyük harfli sözcük önünde sıra sayısı kabul edilen en büyük değer.</summary>
    public const int MaxBeforeCapital = 50;

    private readonly INumberToTurkishWords _numbers;

    /// <summary>Kuralı oluşturur.</summary>
    /// <param name="numbers">Sayı → sözcük çevirici.</param>
    public OrdinalRule(INumberToTurkishWords numbers)
    {
        _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
    }

    /// <inheritdoc />
    public override string Key => NormalizationRuleKeys.Ordinal;

    /// <inheritdoc />
    protected override void ApplyCore(NormalizationContext context) => ReplaceMatches(context, Ordinal(), Evaluate);

    private string? Evaluate(Match m, string input)
    {
        var value = long.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);
        var capital = NextWordIsCapitalized(input, m.Index + m.Length);
        if (capital && value > MaxBeforeCapital)
        {
            return null;
        }

        var ordinal = _numbers.ToOrdinal(value);
        return capital ? TurkishCulture.Capitalize(ordinal) : ordinal;
    }

    [GeneratedRegex(@"(?<![\p{L}\p{N}.,])(?<n>\d{1,9})\.(?=[ \t]+\p{L})", RegexOptions.CultureInvariant)]
    private static partial Regex Ordinal();
}
