namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// Token yenileme isteği (<c>POST api/identity/refresh</c>). Refresh token tek kullanımlıktır (rotating);
/// kullanılmış bir token tekrar gelirse tüm token ailesi iptal edilir.
/// </summary>
public sealed record RefreshRequest
{
    /// <summary>Mevcut refresh token.</summary>
    public string RefreshToken { get; init; } = string.Empty;
}
