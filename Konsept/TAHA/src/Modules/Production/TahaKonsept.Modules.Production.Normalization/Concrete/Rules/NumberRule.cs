using System.Text.RegularExpressions;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Base;
using TahaKonsept.Modules.Production.Normalization.Models;
using TahaKonsept.Modules.Production.Normalization.Text;

namespace TahaKonsept.Modules.Production.Normalization.Concrete.Rules;

/// <summary>
/// 5. adım — sayılar: tam sayılar (trilyonlara kadar), negatif (-5 → eksi beş), ondalık (3,5 → üç virgül beş; 2.5 → iki nokta beş),
/// binlik ayırıcı (1.250.000), yüzde (%40 / 40% → yüzde kırk), aralık (1914-1918 → "… on dört - … on sekiz"),
/// para birimi ($ € ₺ £ TL USD EUR; 10$ → on dolar), kesme ekleri ses uyumuyla (1453'te → bin dört yüz elli üçte, 4'ü → dördü,
/// 100 TL'ye → yüz liraya) ve sıra ekleri (2'nci → ikinci, 5'inci → beşinci). Rakama bitişik birimler (25km) önce ayrılır.
/// Sözcük içindeki rakamlara (MP3, H2O) dokunulmaz.
/// </summary>
public sealed partial class NumberRule : NormalizationRuleBase
{
    private static readonly Lazy<Regex> AttachedUnit = new(() => new Regex(
        "(?<=\\d)(?=(?:" + string.Join('|', MeasurementUnits.AllKeys.Select(Regex.Escape)) + ")(?![\\p{L}\\p{N}]))",
        RegexOptions.CultureInvariant));

    private readonly INumberToTurkishWords _numbers;
    private readonly ITurkishSuffixHarmonizer _harmonizer;

    /// <summary>Kuralı oluşturur.</summary>
    /// <param name="numbers">Sayı → sözcük çevirici.</param>
    /// <param name="harmonizer">Ek uyumlayıcı.</param>
    public NumberRule(INumberToTurkishWords numbers, ITurkishSuffixHarmonizer harmonizer)
    {
        _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
        _harmonizer = harmonizer ?? throw new ArgumentNullException(nameof(harmonizer));
    }

    /// <inheritdoc />
    public override string Key => NormalizationRuleKeys.Number;

    /// <inheritdoc />
    protected override void ApplyCore(NormalizationContext context)
    {
        context.Text = AttachedUnit.Value.Replace(context.Text, " ");
        ReplaceMatches(context, Number(), Evaluate);
    }

    private string Evaluate(Match m, string input)
    {
        var spoken = Read(m.Groups["int"].Value, m.Groups["frac"], m.Groups["dot"]);
        var hasRange = m.Groups["int2"].Success;
        if (hasRange)
        {
            spoken += " - " + Read(m.Groups["int2"].Value, m.Groups["frac2"], null);
        }
        else if (m.Groups["sign"].Success)
        {
            spoken = "eksi " + spoken;
        }

        var isPercent = m.Groups["pctpre"].Success || m.Groups["pctpost"].Success;
        if (isPercent)
        {
            spoken = "yüzde " + spoken;
        }

        var currency = m.Groups["curpre"].Success ? m.Groups["curpre"].Value : m.Groups["curpost"].Value;
        if (currency.Length > 0)
        {
            spoken += " " + CurrencyWord(currency);
        }

        var suffix = m.Groups["suf"];
        if (!suffix.Success)
        {
            return SpaceAfter(input, m.Index + m.Length, spoken);
        }

        var ordinal = OrdinalSuffix().Match(TurkishCulture.ToLower(suffix.Value));
        if (ordinal.Success && !isPercent && currency.Length == 0)
        {
            return _harmonizer.Attach(_numbers.ToOrdinalWords(spoken), ordinal.Groups["rest"].Value);
        }

        return _harmonizer.Attach(spoken, suffix.Value);
    }

    private string Read(string integer, Group? comma, Group? dot)
    {
        var digits = integer.Replace(".", string.Empty, StringComparison.Ordinal);
        if (comma is { Success: true })
        {
            return _numbers.ToDecimalWords(digits, comma.Value, "virgül");
        }

        return dot is { Success: true } ? _numbers.ToDecimalWords(digits, dot.Value, "nokta") : _numbers.ToWords(digits);
    }

    private static string CurrencyWord(string symbol) => symbol switch
    {
        "$" or "USD" => "dolar",
        "€" or "EUR" => "avro",
        "£" => "sterlin",
        _ => "lira",
    };

    [GeneratedRegex(
        @"(?<![\p{L}\p{N}_.,])(?:(?<curpre>[$€₺£])\s?)?(?<pctpre>%\s?)?(?<sign>[-−](?=\d))?" +
        @"(?<int>\d{1,3}(?:\.\d{3})+(?!\d|\.\d)|\d+)(?:,(?<frac>\d+)|\.(?<dot>\d+)(?!\d|\.\d))?" +
        @"(?:[ ]?[-–][ ]?(?<int2>\d{1,3}(?:\.\d{3})+(?!\d|\.\d)|\d+)(?:,(?<frac2>\d+))?)?" +
        @"(?<pctpost>[ ]?%)?(?:[ ]?(?<curpost>[$€₺£]|TL|USD|EUR)(?![\p{L}]))?(?:['’](?<suf>\p{L}+))?(?![\p{L}\p{N}_])",
        RegexOptions.CultureInvariant)]
    private static partial Regex Number();

    [GeneratedRegex(@"^[ıiuü]?nc[ıiuü](?<rest>\p{L}*)$", RegexOptions.CultureInvariant)]
    private static partial Regex OrdinalSuffix();
}
