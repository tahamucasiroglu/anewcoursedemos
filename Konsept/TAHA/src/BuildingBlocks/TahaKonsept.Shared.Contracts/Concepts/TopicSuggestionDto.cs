namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Önerilen tek konu: başlık ve tek satırlık kanca (izleyiciyi ilk 2 saniyede yakalayacak fikir).
/// </summary>
public sealed record TopicSuggestionDto
{
    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Tek satırlık kanca.</summary>
    public string Hook { get; init; } = string.Empty;
}
