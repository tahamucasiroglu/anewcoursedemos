namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// Kayıt isteği (<c>POST api/identity/register</c>, anonim). Her zaman normal kullanıcı açar (yönetici hesabı ayrıca tohumlanır);
/// yönetici yeni kayıtları kapattıysa reddedilir. Başarıda <see cref="TokenResponse"/> döner (kullanıcı doğrudan oturum açar).
/// </summary>
public sealed record RegisterRequest
{
    /// <summary>Kullanıcı adı (3-64 karakter; e-posta biçiminde de olabilir).</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Parola (8-128 karakter).</summary>
    public string Password { get; init; } = string.Empty;
}
