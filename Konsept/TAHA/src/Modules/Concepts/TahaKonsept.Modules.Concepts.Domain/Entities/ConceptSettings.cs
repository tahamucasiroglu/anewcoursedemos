using TahaKonsept.BuildingBlocks.Domain.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Base;
using TahaKonsept.Modules.Concepts.Domain.ValueObjects;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Concepts.Domain.Entities;

/// <summary>
/// Kullanıcı başına konsept üretim ayarları (tek kayıt): modeller ve efor, kelime tekrar denetimi, ince ayar ve gerçek bilgi.
/// Oluşturma: <see cref="Create"/>; güncelleme: <see cref="Update"/>; değer nesnesi olarak okuma: <see cref="ToPreferences"/>.
/// </summary>
public sealed class ConceptSettings : AggregateRoot, IHasUser, IAuditable
{
    /// <summary>EF Core için boş kurucu.</summary>
    private ConceptSettings()
    {
    }

    private ConceptSettings(Guid userId)
    {
        UserId = userId;
    }

    /// <summary>Sahip kullanıcı (benzersiz).</summary>
    public Guid UserId { get; private set; }

    /// <summary>Konsept modeli.</summary>
    public string ConceptModelKey { get; private set; } = string.Empty;

    /// <summary>Konsept eforu.</summary>
    public EffortLevel? ConceptEffort { get; private set; }

    /// <summary>Konu önerisi modeli.</summary>
    public string TopicModelKey { get; private set; } = string.Empty;

    /// <summary>Kelime denetimi modeli.</summary>
    public string WordCheckModelKey { get; private set; } = string.Empty;

    /// <summary>Kelime tekrar denetimi.</summary>
    public bool WordRepetitionCheck { get; private set; }

    /// <summary>Ton.</summary>
    public string? Tone { get; private set; }

    /// <summary>İzleyici.</summary>
    public string? Audience { get; private set; }

    /// <summary>Anlatım biçimi.</summary>
    public string? NarrativeStyle { get; private set; }

    /// <summary>Serbest not.</summary>
    public string? Notes { get; private set; }

    /// <summary>Gerçek bilgi (null = otomatik).</summary>
    public bool? FactGrounding { get; private set; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Son güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Kullanıcı için ayar kaydı oluşturur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="preferences">Tercihler.</param>
    /// <returns>Kayıt.</returns>
    public static ConceptSettings Create(Guid userId, ConceptPreferences preferences)
    {
        var settings = new ConceptSettings(userId);
        settings.Update(preferences);
        return settings;
    }

    /// <summary>Tercihleri günceller (normalleştirilerek).</summary>
    /// <param name="preferences">Tercihler.</param>
    public void Update(ConceptPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        var p = preferences.Normalize();
        ConceptModelKey = p.ConceptModelKey;
        ConceptEffort = p.ConceptEffort;
        TopicModelKey = p.TopicModelKey;
        WordCheckModelKey = p.WordCheckModelKey;
        WordRepetitionCheck = p.WordRepetitionCheck;
        Tone = p.Tone;
        Audience = p.Audience;
        NarrativeStyle = p.NarrativeStyle;
        Notes = p.Notes;
        FactGrounding = p.FactGrounding;
    }

    /// <summary>Değer nesnesine çevirir.</summary>
    /// <returns>Tercihler.</returns>
    public ConceptPreferences ToPreferences() => new(
        ConceptModelKey,
        ConceptEffort,
        TopicModelKey,
        WordCheckModelKey,
        WordRepetitionCheck,
        Tone,
        Audience,
        NarrativeStyle,
        Notes,
        FactGrounding);
}
