namespace TahaKonsept.Shared.Contracts.Providers;

/// <summary>
/// API anahtarı ekleme / değiştirme isteği (<c>PUT api/providers/keys</c>). Sunucuda AES-256-GCM ile şifrelenir.
/// </summary>
public sealed record SaveApiKeyRequest
{
    /// <summary>Platform kimliği (<see cref="ProviderPlatforms"/>).</summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>Anahtar (düz metin; yalnızca bu istekte taşınır). Yerel LLM'de boş olabilir (boşsa kayıtlı anahtar korunur).</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Bölge (yalnızca Azure Speech için zorunlu).</summary>
    public string? Region { get; init; }

    /// <summary>Kök adres (yalnızca Yerel LLM için zorunlu, ör. <c>http://localhost:11434/v1</c>); şifreli saklanır.</summary>
    public string? BaseUrl { get; init; }
}
