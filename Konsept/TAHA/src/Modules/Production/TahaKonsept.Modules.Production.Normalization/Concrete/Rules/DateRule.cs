using System.Globalization;
using System.Text.RegularExpressions;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Base;
using TahaKonsept.Modules.Production.Normalization.Models;
using TahaKonsept.Modules.Production.Normalization.Text;

namespace TahaKonsept.Modules.Production.Normalization.Concrete.Rules;

/// <summary>
/// 2. adım — tarihler, dönemler ve saat:
/// <list type="bullet">
/// <item>MÖ 330 / M.Ö. 330 → "milattan önce üç yüz otuz"; MS 1071 / M.S. → "milattan sonra …" (MS yalnız sayı/Romen rakamı önünde).</item>
/// <item>29.05.1453 ve 29/05/1453 → "yirmi dokuz Mayıs bin dört yüz elli üç" (geçersiz gün/ay dokunulmaz).</item>
/// <item>29 Mayıs 1453'te → "yirmi dokuz Mayıs bin dört yüz elli üçte"; Mayıs 1453; 3 Mart'ta.</item>
/// <item>1453 yılında → "bin dört yüz elli üç yılında"; 14:30 → "on dört otuz", 09:05 → "dokuz sıfır beş".</item>
/// </list>
/// </summary>
public sealed partial class DateRule : NormalizationRuleBase
{
    private readonly INumberToTurkishWords _numbers;
    private readonly ITurkishSuffixHarmonizer _harmonizer;

    /// <summary>Kuralı oluşturur.</summary>
    /// <param name="numbers">Sayı → sözcük çevirici.</param>
    /// <param name="harmonizer">Ek uyumlayıcı.</param>
    public DateRule(INumberToTurkishWords numbers, ITurkishSuffixHarmonizer harmonizer)
    {
        _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
        _harmonizer = harmonizer ?? throw new ArgumentNullException(nameof(harmonizer));
    }

    /// <inheritdoc />
    public override string Key => NormalizationRuleKeys.Date;

    /// <inheritdoc />
    protected override void ApplyCore(NormalizationContext context)
    {
        ReplaceMatches(context, Era(), EvaluateEra);
        ReplaceMatches(context, NumericDate(), EvaluateNumericDate);
        ReplaceMatches(context, MonthDate(), EvaluateMonthDate);
        ReplaceMatches(context, YearContext(), (m, _) => WithSuffix(m.Groups["y"].Value, null));
        ReplaceMatches(context, Time(), EvaluateTime);
    }

    private string? EvaluateEra(Match m, string input)
    {
        var before = m.Groups["era"].Value.Contains('Ö', StringComparison.Ordinal);
        var phrase = before ? "milattan önce" : "milattan sonra";
        var year = m.Groups["y"];
        if (year.Success)
        {
            return phrase + " " + WithSuffix(year.Value, m.Groups["suf"]);
        }

        if (!before && !RomanAhead().IsMatch(input, m.Index + m.Length))
        {
            return null;
        }

        return phrase;
    }

    private string? EvaluateNumericDate(Match m, string input)
    {
        var day = int.Parse(m.Groups["d"].Value, CultureInfo.InvariantCulture);
        var month = int.Parse(m.Groups["m"].Value, CultureInfo.InvariantCulture);
        if (day is < 1 or > 31 || month is < 1 or > 12)
        {
            return null;
        }

        return _numbers.ToWords(day) + " " + TurkishMonths.Names[month] + " " + WithSuffix(m.Groups["y"].Value, m.Groups["suf"]);
    }

    private string? EvaluateMonthDate(Match m, string input)
    {
        var dayGroup = m.Groups["d"];
        var yearGroup = m.Groups["y"];
        if (!dayGroup.Success && !yearGroup.Success)
        {
            return null;
        }

        if (dayGroup.Success)
        {
            var day = int.Parse(dayGroup.Value, CultureInfo.InvariantCulture);
            if (day is < 1 or > 31)
            {
                return null;
            }
        }

        var parts = new List<string>(3);
        if (dayGroup.Success)
        {
            parts.Add(_numbers.ToWords(dayGroup.Value));
        }

        parts.Add(m.Groups["mon"].Value + m.Groups["monsuf"].Value);
        if (yearGroup.Success)
        {
            parts.Add(WithSuffix(yearGroup.Value, m.Groups["suf"]));
        }

        return string.Join(' ', parts);
    }

    private string? EvaluateTime(Match m, string input)
    {
        var hour = _numbers.ToWords(m.Groups["h"].Value.TrimStart('0') is { Length: > 0 } h ? h : "0");
        var minute = m.Groups["mi"].Value;
        var spoken = minute == "00" ? hour : hour + " " + _numbers.ToWords(minute);
        var suffix = m.Groups["suf"];
        return suffix.Success ? _harmonizer.Attach(spoken, suffix.Value) : spoken;
    }

    private string WithSuffix(string digits, Group? suffix)
    {
        var words = _numbers.ToWords(digits);
        return suffix is { Success: true } ? _harmonizer.Attach(words, suffix.Value) : words;
    }

    [GeneratedRegex(@"(?<![\p{L}\p{N}])(?<era>M\.\s?Ö\.?|MÖ|M\.\s?S\.|MS)(?![\p{L}])(?:\s*(?<y>\d{1,4})(?:['’](?<suf>\p{L}+))?(?![\p{N}.,]\d))?", RegexOptions.CultureInvariant)]
    private static partial Regex Era();

    [GeneratedRegex(@"\G\s*(?:\d|[IVXLCDM]+\.)", RegexOptions.CultureInvariant)]
    private static partial Regex RomanAhead();

    [GeneratedRegex(@"(?<![\p{N}.,/])(?<d>\d{1,2})(?<sep>[./])(?<m>\d{1,2})\k<sep>(?<y>\d{4})(?:['’](?<suf>\p{L}+))?(?![\p{N}])", RegexOptions.CultureInvariant)]
    private static partial Regex NumericDate();

    [GeneratedRegex(@"(?<![\p{L}\p{N}.,])(?:(?<d>\d{1,2})\s+)?(?<mon>" + TurkishMonths.Pattern + @")(?![\p{L}])(?<monsuf>['’]\p{L}+)?(?:\s+(?<y>\d{1,4})(?:['’](?<suf>\p{L}+))?(?![\p{L}\p{N}]|[.,]\d))?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex MonthDate();

    [GeneratedRegex(@"(?<![\p{L}\p{N}.,])(?<y>\d{1,4})(?=\s+(?:yıl|sene)\p{L}*)", RegexOptions.CultureInvariant)]
    private static partial Regex YearContext();

    [GeneratedRegex(@"(?<![\p{N}:.,])(?<h>[01]?\d|2[0-3]):(?<mi>[0-5]\d)(?:['’](?<suf>\p{L}+))?(?![\p{N}:])", RegexOptions.CultureInvariant)]
    private static partial Regex Time();
}
