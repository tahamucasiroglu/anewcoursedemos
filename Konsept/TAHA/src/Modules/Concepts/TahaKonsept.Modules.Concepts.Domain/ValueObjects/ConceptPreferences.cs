using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Concepts.Domain.ValueObjects;

/// <summary>
/// Kullanıcının konsept üretim tercihleri (tek parça taşınır). <see cref="Normalize"/> metinleri kırpar, boşları null yapar,
/// boş model anahtarlarını varsayılana çeker.
/// </summary>
/// <param name="ConceptModelKey">Konsept modeli.</param>
/// <param name="ConceptEffort">Konsept eforu.</param>
/// <param name="TopicModelKey">Konu önerisi modeli.</param>
/// <param name="WordCheckModelKey">Kelime denetimi modeli.</param>
/// <param name="WordRepetitionCheck">Kelime tekrar denetimi açık mı.</param>
/// <param name="Tone">Ton.</param>
/// <param name="Audience">İzleyici.</param>
/// <param name="NarrativeStyle">Anlatım biçimi.</param>
/// <param name="Notes">Serbest not.</param>
/// <param name="FactGrounding">Gerçek bilgi (null = otomatik).</param>
public sealed record ConceptPreferences(
    string ConceptModelKey,
    EffortLevel? ConceptEffort,
    string TopicModelKey,
    string WordCheckModelKey,
    bool WordRepetitionCheck,
    string? Tone,
    string? Audience,
    string? NarrativeStyle,
    string? Notes,
    bool? FactGrounding)
{
    /// <summary>Varsayılan tercihler.</summary>
    public static ConceptPreferences Default { get; } = new(
        Constants.ConceptLimits.DefaultConceptModelKey,
        EffortLevel.Medium,
        Constants.ConceptLimits.DefaultTopicModelKey,
        Constants.ConceptLimits.DefaultWordCheckModelKey,
        false,
        null,
        null,
        null,
        null,
        null);

    /// <summary>Normalleştirilmiş kopya döndürür.</summary>
    /// <returns>Tercihler.</returns>
    public ConceptPreferences Normalize() => this with
    {
        ConceptModelKey = KeyOr(ConceptModelKey, Constants.ConceptLimits.DefaultConceptModelKey),
        TopicModelKey = KeyOr(TopicModelKey, Constants.ConceptLimits.DefaultTopicModelKey),
        WordCheckModelKey = KeyOr(WordCheckModelKey, Constants.ConceptLimits.DefaultWordCheckModelKey),
        Tone = NullIfBlank(Tone),
        Audience = NullIfBlank(Audience),
        NarrativeStyle = NullIfBlank(NarrativeStyle),
        Notes = NullIfBlank(Notes),
    };

    private static string KeyOr(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
