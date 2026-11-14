using TahaKonsept.Modules.Concepts.Domain.ValueObjects;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Mapping;

/// <summary>
/// Ayar eşlemeleri: <see cref="ToPreferences"/> (DTO → değer nesnesi), <see cref="ToDto"/> (değer nesnesi → DTO).
/// </summary>
public static class ConceptSettingsMappings
{
    /// <summary>DTO'yu tercihlere çevirir.</summary>
    /// <param name="dto">DTO.</param>
    /// <returns>Tercihler (normalleştirilmiş).</returns>
    public static ConceptPreferences ToPreferences(this ConceptSettingsDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new ConceptPreferences(
            dto.ConceptModelKey,
            dto.ConceptEffort,
            dto.TopicModelKey,
            dto.WordCheckModelKey,
            dto.WordRepetitionCheck,
            dto.Tone,
            dto.Audience,
            dto.NarrativeStyle,
            dto.Notes,
            dto.FactGrounding).Normalize();
    }

    /// <summary>Tercihleri DTO'ya çevirir.</summary>
    /// <param name="preferences">Tercihler.</param>
    /// <param name="effectiveFactGrounding">Aktif profile göre geçerli gerçek bilgi değeri.</param>
    /// <returns>DTO.</returns>
    public static ConceptSettingsDto ToDto(this ConceptPreferences preferences, bool effectiveFactGrounding)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        return new ConceptSettingsDto
        {
            ConceptModelKey = preferences.ConceptModelKey,
            ConceptEffort = preferences.ConceptEffort,
            TopicModelKey = preferences.TopicModelKey,
            WordCheckModelKey = preferences.WordCheckModelKey,
            WordRepetitionCheck = preferences.WordRepetitionCheck,
            Tone = preferences.Tone,
            Audience = preferences.Audience,
            NarrativeStyle = preferences.NarrativeStyle,
            Notes = preferences.Notes,
            FactGrounding = preferences.FactGrounding,
            EffectiveFactGrounding = effectiveFactGrounding,
        };
    }
}
