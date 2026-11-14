namespace TahaKonsept.Web.Infrastructure.Auth;

/// <summary>
/// Auth cookie'sinin <c>AuthenticationProperties</c> içinde sakladığı token adları ve kimlik sabitleri.
/// Cookie Data Protection ile şifrelidir; token'lar tarayıcı JS'ine hiç görünmez (BFF).
/// </summary>
public static class AuthTokenNames
{
    /// <summary>API erişim token'ı (JWT, 15 dk).</summary>
    public const string AccessToken = "access_token";

    /// <summary>Dönen (rotating) refresh token.</summary>
    public const string RefreshToken = "refresh_token";

    /// <summary>Erişim token'ının bitişi (ISO 8601, UTC).</summary>
    public const string AccessTokenExpiresAt = "access_expires_at";

    /// <summary>Refresh token'ın bitişi (ISO 8601, UTC).</summary>
    public const string RefreshTokenExpiresAt = "refresh_expires_at";

    /// <summary>Yetki claim türü (API'deki <c>permission</c> claim'leri).</summary>
    public const string PermissionClaim = "permission";

    /// <summary>Yönetim paneli için yetki değeri.</summary>
    public const string AdminPermission = "admin.access";

    /// <summary>Yönetim paneli policy adı.</summary>
    public const string AdminPolicy = "AdminAccess";

    /// <summary>Auth cookie adı.</summary>
    public const string CookieName = ".TahaKonsept.Auth";
}
