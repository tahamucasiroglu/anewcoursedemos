using System.Text.RegularExpressions;
using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Domain.Rules;

/// <summary>
/// Konu önerisi biçimi (spec 0014 Req 2–3, BR-3, G-19): başlık boş değil ve en fazla 1 cümle, özet boş değil ve en fazla 3 cümle, sahne
/// detayı yok (başlık/özet dışı alan ya da "Sahne 1" / "Scene 2" kalıbı). Biçim dışı öneri kesilmez, bütünüyle elenir.
/// </summary>
public static partial class SuggestionFormat
{
    /// <summary>Başlıktaki en fazla cümle.</summary>
    public const int MaxTitleSentences = 1;

    /// <summary>Özetteki en fazla cümle.</summary>
    public const int MaxSummarySentences = 3;

    /// <summary>Öneri biçime uygun mu.</summary>
    /// <param name="topic">Aday.</param>
    /// <returns>Uygunsa <c>true</c>.</returns>
    public static bool IsValid(ParsedTopic topic)
    {
        ArgumentNullException.ThrowIfNull(topic);
        var titleSentences = SentenceCount.Of(topic.Title);
        var summarySentences = SentenceCount.Of(topic.Summary);
        return !topic.HasExtraFields
            && titleSentences is >= 1 and <= MaxTitleSentences
            && summarySentences is >= 1 and <= MaxSummarySentences
            && !ScenePattern().IsMatch(topic.Title)
            && !ScenePattern().IsMatch(topic.Summary);
    }

    [GeneratedRegex(@"\b(sahne|scene)\s*\d", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ScenePattern();
}
