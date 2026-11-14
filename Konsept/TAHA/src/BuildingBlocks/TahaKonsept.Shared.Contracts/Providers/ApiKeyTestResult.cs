namespace TahaKonsept.Shared.Contracts.Providers;

/// <summary>
/// Anahtar doğrulama sonucu (<c>POST api/providers/keys/{platform}/test</c>). Ücretsiz/ucuz bir çağrı ile denenir.
/// </summary>
public sealed record ApiKeyTestResult
{
    /// <summary>Platform kimliği.</summary>
    public string Platform { get; init; } = string.Empty;

    /// <summary>Anahtar geçerli mi.</summary>
    public bool Success { get; init; }

    /// <summary>Kullanıcıya gösterilecek Türkçe açıklama.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Sağlayıcının döndürdüğü HTTP durum kodu (ağ hatasında null).</summary>
    public int? StatusCode { get; init; }

    /// <summary>Süre (ms).</summary>
    public long DurationMs { get; init; }
}
