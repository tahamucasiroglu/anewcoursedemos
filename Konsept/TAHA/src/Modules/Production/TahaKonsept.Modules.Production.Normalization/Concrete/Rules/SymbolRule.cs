using System.Text.RegularExpressions;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Base;
using TahaKonsept.Modules.Production.Normalization.Models;

namespace TahaKonsept.Modules.Production.Normalization.Concrete.Rules;

/// <summary>
/// 7. adım — semboller: &amp; → "ve" (her yerde); sayı sözcüğünden sonra kalan para sembolü ($ € ₺ £) → dolar/avro/lira/sterlin; +, =, ×, / yalnız iki sayı arasında → "artı", "eşittir", "çarpı", "bölü".
/// Sayılar bu adımda zaten sözcüğe çevrildiği için "sayı" tespiti sayı sözcükleri (bir … trilyon) üzerinden yapılır;
/// "C++", "A+B" gibi kullanımlara dokunulmaz. Tarihlerdeki "/" önceki adımda işlenmiştir.
/// </summary>
public sealed class SymbolRule : NormalizationRuleBase
{
    private static readonly Lazy<Regex> AmpersandRegex = new(() => new Regex("&", RegexOptions.CultureInvariant));

    private readonly Lazy<Regex> _between;
    private readonly Lazy<Regex> _currency;

    /// <summary>Kuralı oluşturur.</summary>
    /// <param name="numbers">Sayı sözcükleri.</param>
    public SymbolRule(INumberToTurkishWords numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);
        _between = new Lazy<Regex>(() =>
        {
            var words = string.Join('|', numbers.Vocabulary.Append("dörd").OrderByDescending(w => w.Length).Select(Regex.Escape));
            return new Regex(
                "(?<=(?:^|[^\\p{L}])(?:" + words + ")[ ]*)(?<s>[+=×/])(?=[ ]*(?:\\d|" + words + "))",
                RegexOptions.CultureInvariant);
        });
        _currency = new Lazy<Regex>(() =>
        {
            var words = string.Join('|', numbers.Vocabulary.OrderByDescending(w => w.Length).Select(Regex.Escape));
            return new Regex(
                "(?<=(?:^|[^\\p{L}])(?:" + words + ")[ ]?)(?<c>[$€₺£])(?:['’](?<suf>\\p{L}+))?",
                RegexOptions.CultureInvariant);
        });
    }

    /// <inheritdoc />
    public override string Key => NormalizationRuleKeys.Symbol;

    /// <inheritdoc />
    protected override void ApplyCore(NormalizationContext context)
    {
        ReplaceMatches(context, Ampersand(), (m, input) => Spaced(input, m, "ve"));
        ReplaceMatches(context, _between.Value, (m, input) => Spaced(input, m, m.Groups["s"].Value switch
        {
            "+" => "artı",
            "=" => "eşittir",
            "×" => "çarpı",
            _ => "bölü",
        }));
        ReplaceMatches(context, _currency.Value, (m, input) =>
        {
            var word = m.Groups["c"].Value switch
            {
                "$" => "dolar",
                "€" => "avro",
                "£" => "sterlin",
                _ => "lira",
            };
            var suffix = m.Groups["suf"];
            return Spaced(input, m, suffix.Success ? word + suffix.Value : word);
        });
    }

    private static string Spaced(string input, Match m, string word) =>
        SpaceAfter(input, m.Index + m.Length, SpaceBefore(input, m.Index, word));

    private static Regex Ampersand() => AmpersandRegex.Value;
}
