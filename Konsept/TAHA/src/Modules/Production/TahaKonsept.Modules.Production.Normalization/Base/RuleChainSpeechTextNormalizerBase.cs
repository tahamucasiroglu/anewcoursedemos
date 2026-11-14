using System.Text.RegularExpressions;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Normalization.Base;

/// <summary>
/// Kural zinciri çalıştıran normalleştiricilerin tabanı: <see cref="Rules"/> sırayla bağlama uygulanır, sonunda art arda
/// boşluklar teke indirilir. Saf ve deterministik (aynı girdi → aynı çıktı). Somut sınıf kuralları ve <see cref="Supports"/>'u verir.
/// </summary>
public abstract partial class RuleChainSpeechTextNormalizerBase : SpeechTextNormalizerBase, ILanguageSpeechTextNormalizer
{
    /// <summary>Zinciri kurar.</summary>
    /// <param name="rules">Kurallar (uygulama sırasıyla).</param>
    protected RuleChainSpeechTextNormalizerBase(IReadOnlyList<INormalizationRule> rules)
    {
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    /// <summary>Uygulama sırasıyla kurallar.</summary>
    public IReadOnlyList<INormalizationRule> Rules { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract bool Supports(string language);

    /// <inheritdoc />
    protected override NormalizedText NormalizeCore(string text, string language, IReadOnlyList<PronunciationEntry> lexicon)
    {
        var context = new NormalizationContext(text, language, lexicon);
        foreach (var rule in Rules)
        {
            rule.Apply(context);
        }

        var spoken = ExtraSpaces().Replace(context.Text, " ").Trim();
        return new NormalizedText(text, spoken, context.Changes.ToList());
    }

    [GeneratedRegex("[ \\t]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex ExtraSpaces();
}
