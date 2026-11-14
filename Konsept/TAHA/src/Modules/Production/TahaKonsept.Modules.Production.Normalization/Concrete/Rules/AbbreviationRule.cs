using System.Text.RegularExpressions;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Base;
using TahaKonsept.Modules.Production.Normalization.Models;
using TahaKonsept.Modules.Production.Normalization.Text;

namespace TahaKonsept.Modules.Production.Normalization.Concrete.Rules;

/// <summary>
/// 6. adım — kısaltmalar, birimler ve büyük harfli kısaltmalar. Sırasıyla:
/// <list type="number">
/// <item>Noktalı kısaltmalar (<see cref="DottedAbbreviations"/>): yy. → yüzyıl, vb. → ve benzeri, Dr. → doktor; sondaki kısaltma
/// cümleyi bitiriyorsa okunuşta nokta korunur; ekler uyumlanıp bitiştirilir (yy.'da → yüzyılda).</item>
/// <item>Birimler (<see cref="MeasurementUnits"/>): km → kilometre, kg'lık → kilogramlık; belirsiz birimler (m, g, sn, dk …) yalnız
/// sayıdan sonra; °C → derece, °F → derece fahrenhayt.</item>
/// <item>Büyük harfli kısaltmalar: bilinenler (<see cref="KnownAcronyms"/>: NATO → Nato), sesli okunabilen ≥4 harfliler baş harf
/// büyük ("DİKKAT" → "Dikkat"), diğerleri harf harf (<see cref="TurkishLetterNames"/>: TBMM → te be me me, ABD'de → a be de'de).
/// Yalnız I/V/X'ten oluşan geçerli Romen rakamları (II, XV) harf harf okunmaz.</item>
/// </list>
/// </summary>
public sealed partial class AbbreviationRule : NormalizationRuleBase
{
    private static readonly Lazy<Regex> Dotted = new(() => new Regex(
        "(?<![\\p{L}\\p{N}])(?<k>" + Alternation(DottedAbbreviations.Titles.Keys.Concat(DottedAbbreviations.Trailing.Keys)) + ")(?:['’](?<suf>\\p{L}+))?",
        RegexOptions.CultureInvariant));

    private static readonly Lazy<Regex> FreeUnits = new(() => new Regex(
        "(?<![\\p{L}\\p{N}])(?<u>" + Alternation(MeasurementUnits.Free.Keys) + ")(?:['’](?<suf>\\p{L}+))?(?![\\p{L}\\p{N}])",
        RegexOptions.CultureInvariant));

    private readonly ITurkishSuffixHarmonizer _harmonizer;
    private readonly Lazy<Regex> _contextUnits;

    /// <summary>Kuralı oluşturur.</summary>
    /// <param name="numbers">Sayı sözcükleri (birimlerin "sayıdan sonra" bağlamı için).</param>
    /// <param name="harmonizer">Ek uyumlayıcı.</param>
    public AbbreviationRule(INumberToTurkishWords numbers, ITurkishSuffixHarmonizer harmonizer)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        _harmonizer = harmonizer ?? throw new ArgumentNullException(nameof(harmonizer));
        _contextUnits = new Lazy<Regex>(() => new Regex(
            "(?<=(?:\\d|(?<![\\p{L}])(?:" + Alternation(numbers.Vocabulary.Append("buçuk")) + "))[ ]?)(?<u>" +
            Alternation(MeasurementUnits.AfterNumber.Keys) + ")(?:['’](?<suf>\\p{L}+))?(?![\\p{L}\\p{N}])",
            RegexOptions.CultureInvariant));
    }

    /// <inheritdoc />
    public override string Key => NormalizationRuleKeys.Abbreviation;

    /// <inheritdoc />
    protected override void ApplyCore(NormalizationContext context)
    {
        ReplaceMatches(context, Dotted.Value, EvaluateDotted);
        ReplaceMatches(context, FreeUnits.Value, (m, input) => EvaluateUnit(m, input, MeasurementUnits.Free));
        ReplaceMatches(context, _contextUnits.Value, (m, input) => EvaluateUnit(m, input, MeasurementUnits.AfterNumber));
        ReplaceMatches(context, Degree(), EvaluateDegree);
        ReplaceMatches(context, Acronym(), EvaluateAcronym);
    }

    /// <summary>Büyük harfli sözcük sesli okunabilir mi: ≥4 harf, en az bir ünlü, art arda 3+ ünsüz ya da 3+ ünlü yok.</summary>
    /// <param name="upper">Büyük harfli sözcük.</param>
    /// <returns>Okunabilirse <c>true</c>.</returns>
    public static bool IsPronounceable(string upper)
    {
        ArgumentNullException.ThrowIfNull(upper);
        if (upper.Length < 4)
        {
            return false;
        }

        var lower = TurkishCulture.ToLower(upper);
        int consonants = 0, vowels = 0, vowelCount = 0;
        foreach (var c in lower)
        {
            if (TurkishCulture.IsVowel(c))
            {
                vowelCount++;
                vowels++;
                consonants = 0;
            }
            else
            {
                consonants++;
                vowels = 0;
            }

            if (consonants >= 3 || vowels >= 3)
            {
                return false;
            }
        }

        return vowelCount > 0;
    }

    private string EvaluateDotted(Match m, string input)
    {
        var key = m.Groups["k"].Value;
        var isTitle = DottedAbbreviations.Titles.TryGetValue(key, out var spoken);
        if (!isTitle)
        {
            spoken = DottedAbbreviations.Trailing[key];
        }

        var suffix = m.Groups["suf"];
        var end = m.Index + m.Length;
        if (suffix.Success)
        {
            return _harmonizer.Attach(spoken!, suffix.Value);
        }

        if (!isTitle && IsSentenceEnd(input, end))
        {
            return spoken + ".";
        }

        return SpaceAfter(input, end, spoken!);
    }

    private string EvaluateUnit(Match m, string input, IReadOnlyDictionary<string, string> table)
    {
        var spoken = table[m.Groups["u"].Value];
        var suffix = m.Groups["suf"];
        var result = suffix.Success ? _harmonizer.Attach(spoken, suffix.Value) : spoken;
        return SpaceBefore(input, m.Index, result);
    }

    private string EvaluateDegree(Match m, string input)
    {
        var scale = m.Groups["s"].Value;
        var spoken = scale == "F" ? "derece fahrenhayt" : "derece";
        var suffix = m.Groups["suf"];
        var result = suffix.Success ? _harmonizer.Attach(spoken, suffix.Value) : spoken;
        return SpaceAfter(input, m.Index + m.Length, SpaceBefore(input, m.Index, result));
    }

    private string? EvaluateAcronym(Match m, string input)
    {
        var word = m.Groups["w"].Value;
        string spoken;
        if (KnownAcronyms.Words.TryGetValue(word, out var known))
        {
            spoken = known;
        }
        else if (word.All(c => c is 'I' or 'V' or 'X') && RomanNumerals.TryParse(word, out _))
        {
            return null;
        }
        else
        {
            spoken = IsPronounceable(word) ? TurkishCulture.ToTitle(word) : TurkishLetterNames.Spell(word);
        }

        var suffix = m.Groups["suf"];
        return suffix.Success ? spoken + "'" + _harmonizer.HarmonizeSuffix(spoken, suffix.Value) : spoken;
    }

    private static string Alternation(IEnumerable<string> keys) =>
        string.Join('|', keys.Distinct(StringComparer.Ordinal).OrderByDescending(k => k.Length).ThenBy(k => k, StringComparer.Ordinal).Select(Regex.Escape));

    [GeneratedRegex(@"°[ ]?(?:(?<s>[CF])(?![\p{L}]))?(?:['’](?<suf>\p{L}+))?", RegexOptions.CultureInvariant)]
    private static partial Regex Degree();

    [GeneratedRegex(@"(?<![\p{L}\p{N}_])(?<w>[A-ZÇĞİÖŞÜ]{2,})(?:['’](?<suf>\p{L}+))?(?![\p{L}\p{N}_])", RegexOptions.CultureInvariant)]
    private static partial Regex Acronym();
}
