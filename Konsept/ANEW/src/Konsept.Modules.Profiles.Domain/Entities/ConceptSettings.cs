namespace Konsept.Modules.Profiles.Domain.Entities;

/// <summary>
/// Bir profilin Konseptler sayfası ayarları (spec 0005 Req 8, Ç-3): üç model rolü için model + efor, ince ayar (ton, izleyici,
/// anlatım biçimi) ve kelime denetimi. Profil başına tek satır; satır yoksa varsayılandır (roller seçilmedi, ince ayar
/// "Belirtilmedi", kelime denetimi kapalı). Sahibi profilin sahibidir (BR-1).
/// </summary>
public sealed class ConceptSettings
{
    /// <summary>Gets profil.</summary>
    public Guid ProfileId { get; init; }

    /// <summary>Gets sahibi.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets konsept üretimi modeli (<c>null</c> = seçilmedi).</summary>
    public string? ConceptModel { get; init; }

    /// <summary>Gets konsept üretimi eforu.</summary>
    public string? ConceptEffort { get; init; }

    /// <summary>Gets anlamca tekrar denetimi modeli.</summary>
    public string? DedupeModel { get; init; }

    /// <summary>Gets anlamca tekrar denetimi eforu.</summary>
    public string? DedupeEffort { get; init; }

    /// <summary>Gets konu önerisi modeli.</summary>
    public string? TopicModel { get; init; }

    /// <summary>Gets konu önerisi eforu.</summary>
    public string? TopicEffort { get; init; }

    /// <summary>Gets ton kodu (<c>null</c> = Belirtilmedi).</summary>
    public string? Tone { get; init; }

    /// <summary>Gets izleyici kodu (<c>null</c> = Belirtilmedi).</summary>
    public string? Audience { get; init; }

    /// <summary>Gets anlatım biçimi kodu (<c>null</c> = Belirtilmedi).</summary>
    public string? Narration { get; init; }

    /// <summary>Gets a value indicating whether kelime denetimi açık.</summary>
    public bool WordCheck { get; init; }

    /// <summary>Gets son güncelleme (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
