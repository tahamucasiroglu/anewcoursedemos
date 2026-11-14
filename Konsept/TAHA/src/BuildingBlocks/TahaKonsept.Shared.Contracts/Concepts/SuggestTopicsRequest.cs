namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Başlık önerisi isteği (<c>POST api/concepts/topics/suggest</c>): adet (1–100) ve isteğe bağlı yön (ör. "Bizans'ın son yüzyılı").
/// </summary>
public sealed record SuggestTopicsRequest
{
    /// <summary>Önerilecek başlık sayısı (1–100).</summary>
    public int Count { get; init; } = 10;

    /// <summary>İsteğe bağlı yön / odak.</summary>
    public string? Hint { get; init; }
}
