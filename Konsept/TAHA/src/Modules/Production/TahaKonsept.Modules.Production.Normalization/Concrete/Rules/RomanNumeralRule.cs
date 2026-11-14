using System.Text.RegularExpressions;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Base;
using TahaKonsept.Modules.Production.Normalization.Models;
using TahaKonsept.Modules.Production.Normalization.Text;

namespace TahaKonsept.Modules.Production.Normalization.Concrete.Rules;

/// <summary>
/// 3. adım — Romen rakamlı sıra sayıları: "II. Mehmed" → "İkinci Mehmed", "XV. yüzyıl" → "on beşinci yüzyıl",
/// "I. Dünya Savaşı" → "Birinci Dünya Savaşı", noktasız "XV yüzyıl" da (yalnız yüzyıl/asır/binyıl önünde).
/// Yanlış pozitif koruması: nokta + ardından sözcük şartı (İngilizce "I am", tek "V"/"X" harfi dokunulmaz), tek harfli
/// L/C/D/M asla (baş harf: "M. Kemal", "C. Ronaldo"), geçersiz Romen biçimleri (IIII, IC) reddedilir.
/// Sonraki sözcük büyük harfle başlıyorsa okunuşun baş harfi büyütülür (tr-TR: "İkinci").
/// </summary>
public sealed partial class RomanNumeralRule : NormalizationRuleBase
{
    private readonly INumberToTurkishWords _numbers;

    /// <summary>Kuralı oluşturur.</summary>
    /// <param name="numbers">Sayı → sözcük çevirici.</param>
    public RomanNumeralRule(INumberToTurkishWords numbers)
    {
        _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
    }

    /// <inheritdoc />
    public override string Key => NormalizationRuleKeys.Roman;

    /// <inheritdoc />
    protected override void ApplyCore(NormalizationContext context) => ReplaceMatches(context, Roman(), Evaluate);

    private string? Evaluate(Match m, string input)
    {
        var roman = m.Groups["r"].Value;
        if (roman.Length == 1 && roman[0] is not ('I' or 'V' or 'X'))
        {
            return null;
        }

        if (!RomanNumerals.TryParse(roman, out var value))
        {
            return null;
        }

        var ordinal = _numbers.ToOrdinal(value);
        return NextWordIsCapitalized(input, m.Index + m.Length) ? TurkishCulture.Capitalize(ordinal) : ordinal;
    }

    [GeneratedRegex(@"(?<![\p{L}\p{N}.])(?<r>[IVXLCDM]+)(?:\.(?=\s+[\p{L}\d])|(?=\s+(?:yüzyıl|yy\.|asır|binyıl)))", RegexOptions.CultureInvariant)]
    private static partial Regex Roman();
}
