using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Domain.Rules;

/// <summary>
/// Elle düzenleme kuralları (spec 0015 Req 4, G-20): sahne başlığı ve seslendirme metni boş olamaz, ekran tarifi boş olabilir, süre
/// pozitif tam saniyedir; metinler kırpılır ve uzunluk sınırını aşamaz. Sahne sayısı değişmez (ekleme/silme/sıralama yok).
/// </summary>
public static class SceneEdit
{
    /// <summary>Alan adı: sahne başlığı.</summary>
    public const string TitleField = "title";

    /// <summary>Alan adı: ekran tarifi.</summary>
    public const string VisualField = "visual";

    /// <summary>Alan adı: seslendirme.</summary>
    public const string NarrationField = "narration";

    /// <summary>Alan adı: süre.</summary>
    public const string DurationField = "durationSeconds";

    /// <summary>Geçersiz alanlar.</summary>
    /// <param name="drafts">Sahneler.</param>
    /// <returns>Sorunlar (boşsa geçerli).</returns>
    public static IReadOnlyList<SceneDraftIssue> Issues(IReadOnlyList<SceneDraft> drafts)
    {
        ArgumentNullException.ThrowIfNull(drafts);
        var issues = new List<SceneDraftIssue>();
        for (var i = 0; i < drafts.Count; i++)
        {
            var d = drafts[i];
            if (Clean(d.Title) is not { Length: > 0 and <= ConceptLimits.MaxTitleLength })
            {
                issues.Add(new SceneDraftIssue(i, TitleField));
            }

            if (Clean(d.Visual).Length > ConceptLimits.MaxSceneTextLength)
            {
                issues.Add(new SceneDraftIssue(i, VisualField));
            }

            if (Clean(d.Narration) is not { Length: > 0 and <= ConceptLimits.MaxSceneTextLength })
            {
                issues.Add(new SceneDraftIssue(i, NarrationField));
            }

            if (d.DurationSeconds is not > 0)
            {
                issues.Add(new SceneDraftIssue(i, DurationField));
            }
        }

        return issues;
    }

    /// <summary>Geçerli taslakları sahnelere çevirir (metinler kırpılır).</summary>
    /// <param name="drafts">Sahneler (önce <see cref="Issues"/> ile doğrulanmış).</param>
    /// <returns>Sahneler.</returns>
    public static IReadOnlyList<ConceptScene> ToScenes(IReadOnlyList<SceneDraft> drafts)
    {
        ArgumentNullException.ThrowIfNull(drafts);
        return [.. drafts.Select(d => new ConceptScene(Clean(d.Title), Clean(d.Visual), Clean(d.Narration), d.DurationSeconds ?? 0))];
    }

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;
}
