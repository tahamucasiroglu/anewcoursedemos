using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>
/// Konu önerisi (spec 0014 Req 2, 7): 1 cümlelik başlık + en fazla 3 cümlelik özet; sahne detayı yok (BR-3). Seçim bekler, seçilmeyen
/// "önerilip seçilmemiş" olarak hafızada kalır ve tekrar önerilmez (BR-4).
/// </summary>
public sealed class TopicSuggestion
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

    /// <summary>Gets kısa özet.</summary>
    public required string Summary { get; init; }

    /// <summary>Gets durum.</summary>
    public SuggestionState State { get; init; }

    /// <summary>Gets oluşturulma (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
