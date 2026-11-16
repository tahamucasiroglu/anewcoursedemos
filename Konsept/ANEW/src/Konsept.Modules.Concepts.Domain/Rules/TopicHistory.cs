using System.Text.RegularExpressions;
using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Domain.Rules;

/// <summary>
/// Profil geçmişinden talimat görünümü (spec 0014 Req 5, G-17): aynı anahtarlı başlıklar tekilleşir (en yenisi kalır), en yeni
/// <see cref="ConceptRuleSet.TitleHistoryLimit"/> başlık birebir girer; eskiler için yalnızca sayı ve en sık
/// <see cref="ConceptRuleSet.HistorySummaryKeywords"/> anlamlı kelime (durak kelime ve 3 harften kısa olanlar hariç). Özet yerel
/// hesaplanır; online çağrı yoktur.
/// </summary>
public static partial class TopicHistory
{
    /// <summary>Görünümü kurar.</summary>
    /// <param name="titles">Geçmiş başlıklar (sırasız).</param>
    /// <param name="language">Profil dili.</param>
    /// <param name="rules">Kurallar.</param>
    /// <returns>Görünüm.</returns>
    public static TopicHistoryView Build(IEnumerable<HistoryTitle> titles, string language, ConceptRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(titles);
        ArgumentNullException.ThrowIfNull(rules);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ordered = titles
            .OrderByDescending(t => t.At)
            .Where(t => seen.Add(TitleKey.Of(t.Title, language)))
            .ToList();
        var limit = Math.Max(0, rules.TitleHistoryLimit);
        var verbatim = ordered.Take(limit).ToList();
        var older = ordered.Skip(limit).ToList();
        if (older.Count == 0)
        {
            return new TopicHistoryView(verbatim, 0, []);
        }

        var stop = rules.StopWordsFor(language);
        var keywords = older
            .SelectMany(t => Word().Matches(t.Title).Select(m => TextCase.Lower(m.Value, language)))
            .Where(w => w.Length >= 3 && !stop.Contains(w))
            .GroupBy(w => w, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Take(Math.Max(0, rules.HistorySummaryKeywords))
            .Select(g => g.Key)
            .ToList();
        return new TopicHistoryView(verbatim, older.Count, keywords);
    }

    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex Word();
}
