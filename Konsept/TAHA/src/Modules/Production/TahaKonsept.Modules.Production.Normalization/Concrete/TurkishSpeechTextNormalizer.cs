using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Base;
using TahaKonsept.Modules.Production.Normalization.Concrete.Rules;

namespace TahaKonsept.Modules.Production.Normalization.Concrete;

/// <summary>
/// Türkçe tam zincir (PLAN §3.1): Sözlük (kullanıcı + konsept, sonra yerleşik) → Tarih → Romen rakamı → Sıra sayısı →
/// Sayı → Kısaltma/birim → Sembol. Örnek: "II. Mehmed 29 Mayıs 1453'te İstanbul'u fethetti." →
/// "İkinci Mehmed yirmi dokuz Mayıs bin dört yüz elli üçte İstanbul'u fethetti." Destek: <c>tr</c>, <c>tr-TR</c>, <c>tr_TR</c>.
/// </summary>
public sealed class TurkishSpeechTextNormalizer : RuleChainSpeechTextNormalizerBase
{
    /// <summary>Zinciri kurar.</summary>
    /// <param name="builtInLexicon">Yerleşik sözlük.</param>
    /// <param name="numbers">Sayı → sözcük çevirici.</param>
    /// <param name="harmonizer">Ek uyumlayıcı.</param>
    public TurkishSpeechTextNormalizer(IBuiltInLexiconProvider builtInLexicon, INumberToTurkishWords numbers, ITurkishSuffixHarmonizer harmonizer)
        : base(CreateRules(builtInLexicon, numbers, harmonizer))
    {
    }

    /// <inheritdoc />
    public override string Name => "turkish";

    /// <inheritdoc />
    public override bool Supports(string language)
    {
        var lang = (language ?? string.Empty).Trim();
        return lang.Length == 0
            || lang.Equals("tr", StringComparison.OrdinalIgnoreCase)
            || lang.StartsWith("tr-", StringComparison.OrdinalIgnoreCase)
            || lang.StartsWith("tr_", StringComparison.OrdinalIgnoreCase);
    }

    private static List<INormalizationRule> CreateRules(IBuiltInLexiconProvider builtInLexicon, INumberToTurkishWords numbers, ITurkishSuffixHarmonizer harmonizer)
    {
        ArgumentNullException.ThrowIfNull(builtInLexicon);
        ArgumentNullException.ThrowIfNull(numbers);
        ArgumentNullException.ThrowIfNull(harmonizer);
        return
        [
            new LexiconRule(builtInLexicon.Entries, harmonizer),
            new DateRule(numbers, harmonizer),
            new RomanNumeralRule(numbers),
            new OrdinalRule(numbers),
            new NumberRule(numbers, harmonizer),
            new AbbreviationRule(numbers, harmonizer),
            new SymbolRule(numbers),
        ];
    }
}
