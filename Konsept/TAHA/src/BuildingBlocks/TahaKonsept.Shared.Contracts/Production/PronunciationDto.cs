namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Okunuş sözlüğü kaydı (yazılış → okunuş): kullanıcı kaydı ya da konseptten gelen öneri.
/// </summary>
public sealed record PronunciationDto
{
    /// <summary>Kimlik.</summary>
    public Guid Id { get; init; }

    /// <summary>Yazılış (ör. Google).</summary>
    public string Written { get; init; } = string.Empty;

    /// <summary>Okunuş (ör. Gugıl).</summary>
    public string Spoken { get; init; } = string.Empty;

    /// <summary>Durum.</summary>
    public PronunciationStatus Status { get; init; }

    /// <summary>Öneri mi (web için kısayol).</summary>
    public bool IsSuggestion => Status == PronunciationStatus.Suggested;

    /// <summary>Öneriyi getiren konsept.</summary>
    public Guid? ConceptId { get; init; }

    /// <summary>Kaynak açıklaması (ör. konsept başlığı).</summary>
    public string? Source { get; init; }

    /// <summary>Oluşturma (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Güncelleme (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}
