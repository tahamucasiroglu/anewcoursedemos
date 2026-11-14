using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Common;
using TahaKonsept.Modules.Concepts.Application.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IWordRepetitionAnalyzer"/> ortak davranışı: seslendirme kelimeleri katlanır, <see cref="MinWordLength"/>'ten kısa ve
/// dolgu kelimeleri atlanır, <see cref="RootLength"/> karakterlik köke göre sayılır (başlıktaki konu kelimeleri hariç).
/// Eşik = max(<see cref="MinRepeats"/>, toplam kelime × <see cref="RepeatRatio"/>); en çok <see cref="MaxFindings"/> bulgu.
/// </summary>
public abstract class WordRepetitionAnalyzerBase : IWordRepetitionAnalyzer
{
    private static readonly HashSet<string> DefaultStopWords = new(StringComparer.Ordinal)
    {
        "icin", "gibi", "daha", "sonra", "once", "kadar", "olan", "oldu", "olarak", "olmus", "olur", "olsa", "ancak", "fakat",
        "cunku", "bile", "simdi", "bunu", "bunun", "buna", "sunu", "onun", "onlar", "onlari", "onlarin", "kendi", "kendini",
        "hepsi", "biri", "birisi", "baska", "diger", "ayni", "yine", "artik", "hala", "henuz", "sadece", "yalnizca", "belki",
        "neden", "nasil", "nerede", "hangi", "boyle", "soyle", "oyle", "ise", "iken", "degil", "var", "yok", "the", "that", "this", "with",
    };

    /// <summary>En kısa sayılan kelime.</summary>
    protected virtual int MinWordLength => 4;

    /// <summary>Kök uzunluğu.</summary>
    protected virtual int RootLength => 5;

    /// <summary>En az tekrar eşiği.</summary>
    protected virtual int MinRepeats => 3;

    /// <summary>Toplam kelimeye oranla eşik.</summary>
    protected virtual double RepeatRatio => 0.03;

    /// <summary>En fazla bulgu.</summary>
    protected virtual int MaxFindings => 8;

    /// <summary>Dolgu kelimeleri (katlanmış).</summary>
    protected virtual IReadOnlySet<string> StopWords => DefaultStopWords;

    /// <summary>Belgeyi analiz eder.</summary>
    /// <param name="document">Belge.</param>
    /// <returns>Bulgular.</returns>
    public virtual IReadOnlyList<WordRepetitionFinding> Analyze(ConceptDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var subject = TurkishText.Words(document.Title).Select(w => Root(TurkishText.Fold(w))).ToHashSet(StringComparer.Ordinal);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var forms = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        var scenes = new Dictionary<string, SortedSet<int>>(StringComparer.Ordinal);
        var total = 0;

        for (var i = 0; i < document.Scenes.Count; i++)
        {
            foreach (var word in TurkishText.Words(document.Scenes[i].Narration))
            {
                total++;
                var folded = TurkishText.Fold(word);
                if (folded.Length < MinWordLength || StopWords.Contains(folded) || folded.All(char.IsDigit))
                {
                    continue;
                }

                var root = Root(folded);
                if (subject.Contains(root))
                {
                    continue;
                }

                counts[root] = counts.GetValueOrDefault(root) + 1;
                if (!forms.TryGetValue(root, out var f))
                {
                    forms[root] = f = new Dictionary<string, int>(StringComparer.Ordinal);
                    scenes[root] = [];
                }

                var surface = word.ToLower(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
                f[surface] = f.GetValueOrDefault(surface) + 1;
                scenes[root].Add(i);
            }
        }

        var threshold = Math.Max(MinRepeats, (int)Math.Ceiling(total * RepeatRatio));
        return counts
            .Where(kv => kv.Value >= threshold)
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .Take(MaxFindings)
            .Select(kv => new WordRepetitionFinding(
                forms[kv.Key].OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal).First().Key,
                kv.Value,
                scenes[kv.Key].ToList()))
            .ToList();
    }

    private string Root(string folded) => folded.Length <= RootLength ? folded : folded[..RootLength];
}
