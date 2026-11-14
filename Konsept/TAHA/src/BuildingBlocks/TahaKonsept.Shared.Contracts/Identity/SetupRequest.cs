namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// İlk kurulum isteği (<c>POST api/identity/setup</c>): veritabanında hiç kullanıcı yokken tek yönetici hesabını oluşturur.
/// Başarıda <see cref="TokenResponse"/> döner (kullanıcı doğrudan oturum açar).
/// </summary>
public sealed record SetupRequest
{
    /// <summary>Kullanıcı adı (3-64 karakter).</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Parola (en az 8 karakter).</summary>
    public string Password { get; init; } = string.Empty;
}
