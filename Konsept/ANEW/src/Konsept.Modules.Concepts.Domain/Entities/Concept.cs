using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>
/// Üretilmiş video konsepti (spec 0014 Req 10, 15, 27): başlık ve sıralı sahneler; sahibi ve profili (BR-1, BR-2); üretildiği andaki
/// profil değerleri (dil, biçim, hedef süre) — profil sonradan değişse de bunlar değişmez. Uyarılar: kelime tekrarı (BR-15) ve süre
/// uyumsuzluğu (G-13). <see cref="TitleKey"/> birebir tekrar denetimi içindir (BR-4).
/// </summary>
public sealed class Concept
{
    /// <summary>Gets kimlik (UUIDv7).</summary>
    public Guid Id { get; init; }

    /// <summary>Gets sahibi.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets profil.</summary>
    public Guid ProfileId { get; init; }

    /// <summary>Gets başlık.</summary>
    public required string Title { get; init; }

    /// <summary>Gets birebir tekrar anahtarı.</summary>
    public required string TitleKey { get; init; }

    /// <summary>Gets üretim yolu.</summary>
    public ConceptSource Source { get; init; }

    /// <summary>Gets durum.</summary>
    public ConceptStatus Status { get; init; }

    /// <summary>Gets üretildiği andaki video dili.</summary>
    public required string Language { get; init; }

    /// <summary>Gets üretildiği andaki biçim.</summary>
    public required string Format { get; init; }

    /// <summary>Gets üretildiği andaki hedef süre (saniye).</summary>
    public int TargetDurationSeconds { get; init; }

    /// <summary>Gets sahneler (sıralı).</summary>
    public IReadOnlyList<ConceptScene> Scenes { get; init; } = [];

    /// <summary>Gets a value indicating whether "kelime tekrarı" uyarısı var.</summary>
    public bool WordRepetitionWarning { get; init; }

    /// <summary>Gets a value indicating whether "süre uyumsuz" uyarısı var.</summary>
    public bool DurationMismatchWarning { get; init; }

    /// <summary>Gets oluşturulma (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets son güncelleme (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Gets silinme zamanı (0016; <c>null</c> = silinmedi).</summary>
    public DateTimeOffset? DeletedAt { get; init; }
}
