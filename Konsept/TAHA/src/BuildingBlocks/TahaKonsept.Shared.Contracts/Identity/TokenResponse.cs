namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// Erişim + yenileme token çifti (giriş, yenileme ve cihaz eşleme sonucunda döner).
/// </summary>
public sealed record TokenResponse
{
    /// <summary>JWT erişim token'ı (15 dk).</summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>Dönen (rotating) yenileme token'ı.</summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>Erişim token'ının bitiş anı (UTC).</summary>
    public DateTimeOffset AccessTokenExpiresAtUtc { get; init; }

    /// <summary>Yenileme token'ının bitiş anı (UTC).</summary>
    public DateTimeOffset RefreshTokenExpiresAtUtc { get; init; }

    /// <summary>Token türü (her zaman <c>Bearer</c>).</summary>
    public string TokenType { get; init; } = "Bearer";
}
