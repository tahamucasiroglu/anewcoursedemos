using System.Globalization;
using Konsept.Shared.Kernel.Enums;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Auth.Abstract;
using Konsept.Web.Resources;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;

namespace Konsept.Web.Auth;

/// <summary>
/// Web oturum cookie'sinin olayları (spec 0002 Req 11, 13, 15): her istekte oturum Api'ye doğrulatılır — iptal edilmiş ya da
/// süresi dolmuş oturum bir sonraki istekte düşer (diğer tarayıcıdan çıkış dahil); erişim token'ı bitmek üzereyse tek uçuşlu
/// refresh yapılır; giriş gerektiren istekte sayfa giriş sayfasına (oturum düştüyse "Oturumunuz sona erdi" bilgisiyle),
/// Ajax isteği 401 JSON'a gider — hiçbir işlem sessizce başarısız olmaz. Çerçeve adaptörü; token'lar yalnızca şifreli
/// cookie biletinde durur.
/// </summary>
/// <param name="coordinator">Refresh koordinatörü.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
public sealed class KonseptCookieEvents(ITokenRefreshCoordinator coordinator, TimeProvider timeProvider) : CookieAuthenticationEvents
{
    /// <summary>Cookie adı.</summary>
    public const string CookieName = "konsept.auth";

    /// <summary>Doğrulanmış erişim token'ının isteğe bırakıldığı öğe anahtarı.</summary>
    public const string AccessTokenItem = "konsept.access_token";

    /// <summary>Bilet içindeki erişim token'ı adı.</summary>
    public const string AccessTokenName = "access_token";

    /// <summary>Bilet içindeki refresh token adı.</summary>
    public const string RefreshTokenName = "refresh_token";

    /// <summary>Bilet içindeki erişim token'ı bitiş zamanı adı.</summary>
    public const string ExpiresAtName = "expires_at";

    /// <summary>Giriş sayfasına "oturum bitti" bilgisini taşıyan sorgu parametresi.</summary>
    public const string SessionEndedQuery = "oturum";

    /// <summary>"Oturum bitti" değeri.</summary>
    public const string SessionEndedValue = "bitti";

    /// <summary>Ajax isteğine dönen hata kodu.</summary>
    public const string SessionExpiredCode = "auth.session_expired";

    /// <summary>Erişim token'ı bu süreden az kaldıysa önceden yenilenir.</summary>
    public static readonly TimeSpan RefreshAhead = TimeSpan.FromSeconds(60);

    /// <summary>Token çiftini bilete yazar.</summary>
    /// <param name="properties">Bilet özellikleri.</param>
    /// <param name="session">Token çifti.</param>
    public static void StoreSession(AuthenticationProperties properties, AuthSessionResponse session)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(session);
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = AccessTokenName, Value = session.AccessToken },
            new AuthenticationToken { Name = RefreshTokenName, Value = session.RefreshToken },
            new AuthenticationToken { Name = ExpiresAtName, Value = session.AccessTokenExpiresAt.ToString("O", CultureInfo.InvariantCulture) },
        ]);
    }

    /// <summary>İstek Ajax/JSON isteği mi (jQuery <c>X-Requested-With</c> başlığı ya da JSON kabulü).</summary>
    /// <param name="request">HTTP isteği.</param>
    /// <returns>Ajax ise <c>true</c>.</returns>
    public static bool IsAjax(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return string.Equals(request.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.Ordinal)
            || request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var http = context.HttpContext;
        var access = context.Properties.GetTokenValue(AccessTokenName);
        var refresh = context.Properties.GetTokenValue(RefreshTokenName);
        if (string.IsNullOrEmpty(access) || string.IsNullOrEmpty(refresh))
        {
            await RejectAsync(context).ConfigureAwait(false);
            return;
        }

        var api = http.RequestServices.GetRequiredService<IKonseptApiClient>();
        if (ExpiresSoon(context.Properties.GetTokenValue(ExpiresAtName)))
        {
            var refreshed = await coordinator.RefreshAsync(api, refresh, http.RequestAborted).ConfigureAwait(false);
            if (refreshed.IsSuccess)
            {
                StoreSession(context.Properties, refreshed.Value);
                context.ShouldRenew = true;
                access = refreshed.Value.AccessToken;
            }
            else if (refreshed.Error!.Type == ErrorType.Unauthorized)
            {
                await RejectAsync(context).ConfigureAwait(false);
                return;
            }
        }

        var session = await api.GetSessionAsync(access, http.RequestAborted).ConfigureAwait(false);
        if (!session.IsSuccess && session.Error!.Type == ErrorType.Unauthorized)
        {
            await RejectAsync(context).ConfigureAwait(false);
            return;
        }

        // Api erişilemiyorsa oturum düşürülmez: sayfa "API erişilemiyor" gösterir.
        http.Items[AccessTokenItem] = access;
    }

    /// <inheritdoc />
    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var request = context.Request;
        if (IsAjax(request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return context.Response.WriteAsJsonAsync(new { code = SessionExpiredCode, message = UiText.SessionExpired });
        }

        // Yalnızca GET isteği dönüş adresi olur (review R1-5): ör. düşmüş oturumla POST /cikis girişten sonra GET /cikis'e (404)
        // dönmesin.
        var login = HttpMethods.IsGet(request.Method)
            ? context.RedirectUri
            : $"{request.PathBase}{context.Options.LoginPath}";
        var target = request.Cookies.ContainsKey(CookieName)
            ? QueryHelpers.AddQueryString(login, SessionEndedQuery, SessionEndedValue)
            : login;
        context.Response.Redirect(target);
        return Task.CompletedTask;
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
    }

    private bool ExpiresSoon(string? expiresAt) =>
        !DateTimeOffset.TryParse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expires)
        || expires - timeProvider.GetUtcNow() < RefreshAhead;
}
