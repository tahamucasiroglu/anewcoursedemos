using System.Text;
using System.Text.RegularExpressions;
using TahaKonsept.Modules.Production.Normalization.Abstract;
using TahaKonsept.Modules.Production.Normalization.Base;
using TahaKonsept.Modules.Production.Normalization.Models;
using TahaKonsept.Modules.Production.Normalization.Text;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Normalization.Concrete.Rules;

/// <summary>
/// 1. adım — okunuş sözlüğü. Önce bağlamdaki kullanıcı + konsept girdileri, sonra yerleşik sözlük (önce gelen kazanır).
/// Bütün sözcük eşleşmesi, <c>tr-TR</c> ile büyük/küçük harf duyarsız (ı/i katlanır: "Instagram" = "İnstagram"); uzun girdi
/// önce eşleşir ("New York" &gt; "York"). Kesme ekleri korunur ve uyumlanır: "Google'ın" → "Gugıl'ın".
/// Uyumlayıcı verilmezse (Türkçe dışı diller) ek aynen kalır. Yerleşik-sözlük deseni önbelleklenir.
/// </summary>
public sealed class LexiconRule : NormalizationRuleBase
{
    private readonly IReadOnlyList<PronunciationEntry> _builtIn;
    private readonly ITurkishSuffixHarmonizer? _harmonizer;
    private readonly Lazy<LexiconLookup?> _builtInLookup;

    /// <summary>Kuralı oluşturur.</summary>
    /// <param name="builtIn">Yerleşik girdiler (boş olabilir).</param>
    /// <param name="harmonizer">Türkçe ek uyumlayıcı (<c>null</c> = ekleri aynen bırak).</param>
    public LexiconRule(IReadOnlyList<PronunciationEntry> builtIn, ITurkishSuffixHarmonizer? harmonizer)
    {
        _builtIn = builtIn ?? [];
        _harmonizer = harmonizer;
        _builtInLookup = new Lazy<LexiconLookup?>(() => Build([], _builtIn), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public override string Key => NormalizationRuleKeys.Lexicon;

    /// <inheritdoc />
    protected override void ApplyCore(NormalizationContext context)
    {
        var lookup = context.Lexicon.Count == 0 ? _builtInLookup.Value : Build(context.Lexicon, _builtIn);
        if (lookup is null)
        {
            return;
        }

        var input = context.Text;
        var folded = TurkishCulture.FoldForMatch(input);
        var sb = new StringBuilder(input.Length + 16);
        var last = 0;
        foreach (Match m in lookup.Pattern.Matches(folded))
        {
            var word = m.Groups["w"];
            if (!lookup.Spoken.TryGetValue(CollapseSpaces(word.Value), out var spoken))
            {
                continue;
            }

            var replacement = spoken;
            var suffix = m.Groups["suf"];
            if (suffix.Success)
            {
                var originalSuffix = input.Substring(suffix.Index, suffix.Length);
                var apostrophe = input[suffix.Index - 1];
                replacement += _harmonizer is null
                    ? apostrophe + originalSuffix
                    : "'" + _harmonizer.HarmonizeSuffix(spoken, originalSuffix);
            }

            var original = input.Substring(m.Index, m.Length);
            sb.Append(input, last, m.Index - last).Append(replacement);
            last = m.Index + m.Length;
            context.AddChange(original, replacement, Key);
        }

        if (last == 0)
        {
            return;
        }

        sb.Append(input, last, input.Length - last);
        context.Text = sb.ToString();
    }

    private static LexiconLookup? Build(IReadOnlyList<PronunciationEntry> first, IReadOnlyList<PronunciationEntry> second)
    {
        var spoken = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in first.Concat(second))
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Written) || string.IsNullOrWhiteSpace(entry.Spoken))
            {
                continue;
            }

            spoken.TryAdd(CollapseSpaces(TurkishCulture.FoldForMatch(entry.Written.Trim())), entry.Spoken.Trim());
        }

        if (spoken.Count == 0)
        {
            return null;
        }

        var alternatives = spoken.Keys
            .OrderByDescending(k => k.Length)
            .ThenBy(k => k, StringComparer.Ordinal)
            .Select(k => Regex.Escape(k).Replace("\\ ", "\\s+", StringComparison.Ordinal));
        var pattern = $"(?<![\\p{{L}}\\p{{N}}_])(?<w>{string.Join('|', alternatives)})(?:[{Apostrophes}](?<suf>\\p{{L}}+))?(?![\\p{{L}}\\p{{N}}_])";
        return new LexiconLookup(new Regex(pattern, RegexOptions.CultureInvariant), spoken);
    }

    private static string CollapseSpaces(string value) => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
