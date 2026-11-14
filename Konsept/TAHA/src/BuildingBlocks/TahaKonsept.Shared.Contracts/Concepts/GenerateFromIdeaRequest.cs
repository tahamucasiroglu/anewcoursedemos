namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Serbest fikirden konsept üretimi (<c>POST api/concepts/from-idea</c>): fikir metni ve adet (1–100; her biri farklı açı).
/// </summary>
public sealed record GenerateFromIdeaRequest
{
    /// <summary>Fikir metni.</summary>
    public string Idea { get; init; } = string.Empty;

    /// <summary>Üretilecek konsept sayısı (1–100).</summary>
    public int Count { get; init; } = 1;
}
