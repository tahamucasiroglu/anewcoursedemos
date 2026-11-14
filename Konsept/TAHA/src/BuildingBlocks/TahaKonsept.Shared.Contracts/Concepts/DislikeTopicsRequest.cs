namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Beğenilmeyen önerilen başlıklar (<c>POST api/concepts/topics/dislike</c>): konu hafızasına negatif örnek olarak yazılır.
/// </summary>
public sealed record DislikeTopicsRequest
{
    /// <summary>Başlıklar.</summary>
    public IReadOnlyList<string> Titles { get; init; } = [];
}
