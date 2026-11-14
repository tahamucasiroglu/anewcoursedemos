namespace TahaKonsept.Shared.Contracts.Providers;

/// <summary>
/// Ayarlar → API anahtarları satırı (<c>GET api/providers/keys</c> öğesi; her online platform için bir satır).
/// Anahtarın kendisi asla dönmez; yalnızca son 4 hane.
/// </summary>
public sealed record ApiKeyInfo
{
    /// <summary>Platform kimliği (<see cref="ProviderPlatforms"/>).</summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>Görünen ad.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Bu anahtarın hangi işlerde kullanıldığı.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Bölge alanı gerekiyor mu (Azure Speech).</summary>
    public bool RequiresRegion { get; init; }

    /// <summary>Anahtar kayıtlı mı.</summary>
    public bool Configured { get; init; }

    /// <summary>Son 4 hane (kayıtlı değilse null).</summary>
    public string? Last4 { get; init; }

    /// <summary>Bölge (Azure Speech, ör. <c>westeurope</c>).</summary>
    public string? Region { get; init; }

    /// <summary>Son güncelleme zamanı (UTC).</summary>
    public DateTimeOffset? UpdatedAtUtc { get; init; }

    /// <summary>Son test sonucu (test edilmediyse null).</summary>
    public bool? LastTestSucceeded { get; init; }

    /// <summary>Son test zamanı (UTC).</summary>
    public DateTimeOffset? LastTestedAtUtc { get; init; }
}
