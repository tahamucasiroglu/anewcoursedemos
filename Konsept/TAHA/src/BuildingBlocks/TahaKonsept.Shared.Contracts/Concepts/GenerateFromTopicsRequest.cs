namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Seçilen başlıklardan konsept üretimi (<c>POST api/concepts/topics/generate</c>): 1–100 başlık; arka plan işi olarak çalışır.
/// </summary>
public sealed record GenerateFromTopicsRequest
{
    /// <summary>Başlıklar.</summary>
    public IReadOnlyList<string> Titles { get; init; } = [];
}
