namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// Giriş isteği (<c>POST api/identity/login</c>): kullanıcı adı ve parola. Başarıda <see cref="TokenResponse"/> döner.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>Kullanıcı adı.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Parola.</summary>
    public string Password { get; init; } = string.Empty;
}
