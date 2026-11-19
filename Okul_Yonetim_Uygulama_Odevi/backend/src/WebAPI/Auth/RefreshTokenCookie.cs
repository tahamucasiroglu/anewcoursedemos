using OkulYonetim.Domain.Identity;

namespace OkulYonetim.WebAPI.Auth;

/// <summary>
/// Web'de yenileme anahtarı çerezi: HttpOnly (JavaScript okuyamaz), SameSite=Strict (başka siteden gönderilmez),
/// yalnızca <c>/api/auth</c> yoluna gider, 7 gün. HTTPS'te Secure işaretlenir (Pi düz HTTP — backlog B-13).
/// Mobil çerez kullanmaz; anahtarı yanıt gövdesinde alır.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "okul_refresh";
    public const string Path = "/api/auth";

    public static void Write(HttpResponse response, string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Cookies.Append(Name, refreshToken, Options(response.HttpContext.Request, DateTimeOffset.UtcNow.Add(UserSession.IdleLifetime)));
    }

    public static void Delete(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        response.Cookies.Delete(Name, Options(response.HttpContext.Request, null));
    }

    public static string? Read(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Cookies.TryGetValue(Name, out string? value) ? value : null;
    }

    private static CookieOptions Options(HttpRequest request, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Secure = request.IsHttps,
        Expires = expires,
        IsEssential = true,
    };
}
