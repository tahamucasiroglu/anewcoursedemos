namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Konseptin doğrudan düzenlenmesi (<c>PUT api/concepts/{id}</c>): yeni belge; <see cref="Title"/> boşsa belgenin başlığı kullanılır.
/// Dil, biçim ve hedef süre gibi kanal alanları mevcut belgeden korunur.
/// </summary>
public sealed record UpdateConceptRequest
{
    /// <summary>Başlık (isteğe bağlı).</summary>
    public string? Title { get; init; }

    /// <summary>Düzenlenmiş belge.</summary>
    public ConceptDocument Document { get; init; } = new();
}
