namespace TahaKonsept.Web.Models;

/// <summary>Tarayıcıdan gelen giriş isteği (Ajax JSON): kullanıcı adı, parola, beni hatırla, dönüş adresi.</summary>
public sealed record LoginRequest
{
    /// <summary>Kullanıcı adı.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Parola.</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>Kalıcı oturum (tarayıcı kapansa da açık kalsın).</summary>
    public bool RememberMe { get; init; } = true;

    /// <summary>Girişten sonra dönülecek yerel adres.</summary>
    public string? ReturnUrl { get; init; }
}
