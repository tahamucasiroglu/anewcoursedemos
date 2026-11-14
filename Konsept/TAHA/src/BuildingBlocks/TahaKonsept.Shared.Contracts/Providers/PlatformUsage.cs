namespace TahaKonsept.Shared.Contracts.Providers;

/// <summary>
/// Aylık harcamanın platform kırılımı satırı.
/// </summary>
public sealed record PlatformUsage
{
    /// <summary>Platform kimliği.</summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>Toplam maliyet ($).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Çağrı sayısı.</summary>
    public int Calls { get; init; }
}
