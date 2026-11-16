using System.Text.RegularExpressions;
using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Domain.Rules;

/// <summary>
/// Kelime denetimi (BR-15, spec 0014 Req 14, S-2): seslendirme metinleri model çağrısı olmadan sayılır. Kelimeler dile göre küçük harfe
/// çevrilir (Türkçe I/İ), kesme işaretli ekler kelimenin parçasıdır. Durak kelimeler hariç bir kelime <b>en az</b>
/// <see cref="ConceptRuleSet.WordRepeatMinCount"/> kez geçer <b>ve</b> toplam kelimelerin (durak kelimeler dahil)
/// <see cref="ConceptRuleSet.WordRepeatMinShare"/> oranını <b>aşarsa</b> eşik aşılmıştır.
/// </summary>
public static partial class WordRepetition
{
    /// <summary>Eşik aşıldı mı.</summary>
    /// <param name="narrations">Seslendirme metinleri.</param>
    /// <param name="language">Profil dili.</param>
    /// <param name="rules">Kurallar.</param>
    /// <returns>Aşıldıysa <c>true</c>.</returns>
    public static bool Exceeds(IEnumerable<string> narrations, string language, ConceptRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(narrations);
        ArgumentNullException.ThrowIfNull(rules);
        var stop = rules.StopWordsFor(language);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var total = 0;
        foreach (var text in narrations)
        {
            foreach (Match match in Word().Matches(text ?? string.Empty))
            {
                total++;
                var word = TextCase.Lower(match.Value, language);
                if (!stop.Contains(word))
                {
                    counts[word] = counts.GetValueOrDefault(word) + 1;
                }
            }
        }

        return counts.Values.Any(c => c >= rules.WordRepeatMinCount && c > total * rules.WordRepeatMinShare);
    }

    [GeneratedRegex(@"\p{L}+(?:['’]\p{L}+)*")]
    private static partial Regex Word();
}
