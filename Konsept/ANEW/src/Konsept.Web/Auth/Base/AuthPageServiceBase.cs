using System.Globalization;
using System.Security.Claims;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Auth.Abstract;
using Konsept.Web.Models;
using Konsept.Web.Resources;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Auth.Base;

/// <summary>
/// Giriş/kayıt/çıkış akışı (spec 0002 Req 1–12, 15): başarıda 14 günlük kalıcı oturum cookie'si (token'lar şifreli bilette)
/// ve yalnızca site içi dönüş adresi; hatada Api kodu Türkçe metne çevrilir (kalan hak, kilidin İstanbul saatiyle açılışı,
/// e-posta kayıtlı, çok fazla deneme, sunucuya ulaşılamıyor + korelasyon numarası). Girilen ad/e-posta korunur, şifre
/// boşaltılır. Api erişimi alt sınıftadır.
/// </summary>
public abstract partial class AuthPageServiceBase : IAuthPageService
{
    /// <summary>Giriş sayfasının görünümü.</summary>
    public const string LoginView = "Giris";

    /// <summary>Kayıt sayfasının görünümü.</summary>
    public const string RegisterView = "Kayit";

    /// <summary>Çıkış tamamlanamazsa gösterilen hata sayfası.</summary>
    public const string ErrorView = "~/Views/Pages/Hata.cshtml";

    /// <summary>Hata sayfasına özel mesajı taşıyan ViewData anahtarı.</summary>
    public const string ErrorMessageKey = "ErrorMessage";

    /// <summary>Kilit saatinin gösterildiği saat dilimi.</summary>
    public const string DisplayTimeZone = "Europe/Istanbul";

    /// <summary>Gets API istemcisi.</summary>
    protected abstract IKonseptApiClient Api { get; }

    /// <summary>Gets teknik log.</summary>
    protected abstract ILogger Logger { get; }

    /// <summary>Gets refresh koordinatörü (çıkıştan önce dolmuş token'ı yenilemek için).</summary>
    protected abstract ITokenRefreshCoordinator Coordinator { get; }

    /// <summary>Gets saat kaynağı.</summary>
    protected abstract TimeProvider Time { get; }

    /// <inheritdoc />
    public IActionResult ShowLogin(Controller controller, string? returnUrl, bool sessionEnded)
    {
        ArgumentNullException.ThrowIfNull(controller);
        return IsSignedIn(controller)
            ? controller.LocalRedirect("/")
            : controller.View(LoginView, new LoginForm { ReturnUrl = returnUrl, Notice = sessionEnded ? UiText.SessionExpired : null });
    }

    /// <inheritdoc />
    public IActionResult ShowRegister(Controller controller, string? returnUrl)
    {
        ArgumentNullException.ThrowIfNull(controller);
        return IsSignedIn(controller) ? controller.LocalRedirect("/") : controller.View(RegisterView, new RegisterForm { ReturnUrl = returnUrl });
    }

    /// <inheritdoc />
    public async Task<IActionResult> LoginAsync(Controller controller, LoginForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(form);
        var result = await Api.LoginAsync(new LoginApiRequest(form.Email, form.Password), ClientIp(controller), cancellationToken).ConfigureAwait(false);
        return await CompleteAsync(controller, form, LoginView, result).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IActionResult> RegisterAsync(Controller controller, RegisterForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(form);
        var result = await Api.RegisterAsync(new RegisterApiRequest(form.Name, form.Email, form.Password), ClientIp(controller), cancellationToken).ConfigureAwait(false);
        return await CompleteAsync(controller, form, RegisterView, result).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IActionResult> LogoutAsync(Controller controller, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(controller);
        var http = controller.HttpContext;
        var access = await http.GetTokenAsync(KonseptCookieEvents.AccessTokenName).ConfigureAwait(false);
        AuthSessionResponse? renewed = null;
        if (!string.IsNullOrEmpty(access))
        {
            // Oturum refresh geçici olarak düştüğü için dolmuş token'la korunduysa (review R2-2) önce yenilenir (review R3-1):
            // dolmuş token'ın 401'i "zaten çıkılmış" sayılıp diğer oturumlar açık kalmasın. Refresh 401'i oturumun gerçekten
            // düştüğünü gösterir; geçici refresh hatası çıkış hatasıdır.
            if (ExpiresSoon(await http.GetTokenAsync(KonseptCookieEvents.ExpiresAtName).ConfigureAwait(false)))
            {
                var refresh = await http.GetTokenAsync(KonseptCookieEvents.RefreshTokenName).ConfigureAwait(false);
                var refreshed = string.IsNullOrEmpty(refresh)
                    ? Result<AuthSessionResponse>.Failure(new Error(KonseptCookieEvents.SessionExpiredCode, UiText.SessionExpired, ErrorType.Unauthorized))
                    : await Coordinator.RefreshAsync(Api, refresh, cancellationToken).ConfigureAwait(false);
                if (!refreshed.IsSuccess && refreshed.Error!.Type != ErrorType.Unauthorized)
                {
                    return LogoutFailed(controller, refreshed.Error.Code);
                }

                renewed = refreshed.IsSuccess ? refreshed.Value : null;
                access = renewed?.AccessToken;
            }
        }

        if (!string.IsNullOrEmpty(access))
        {
            var result = await Api.LogoutAsync(access, cancellationToken).ConfigureAwait(false);

            // Api oturumları iptal edemediyse çıkış başarılı gibi gösterilmez (Req 15, review R1-4): oturum kalır, genel hata ve
            // korelasyon numarası gösterilir. Oturum Api'de zaten düşmüşse (geçerli token'a 401) yerel cookie de silinir.
            if (!result.IsSuccess && result.Error!.Type != ErrorType.Unauthorized)
            {
                if (renewed is not null)
                {
                    await KeepRenewedSessionAsync(http, renewed).ConfigureAwait(false); // Dönmüş token çifti kaybolmasın.
                }

                return LogoutFailed(controller, result.Error.Code);
            }
        }

        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
        return controller.LocalRedirect("/giris");
    }

    /// <summary>Kilidin açılma zamanını ekranda gösterilecek biçime çevirir (Europe/Istanbul, <c>HH:mm</c>).</summary>
    /// <param name="lockedUntil">Açılma zamanı.</param>
    /// <returns>Saat metni.</returns>
    public static string FormatUnlockTime(DateTimeOffset lockedUntil)
    {
        // Saniyesi olan açılış bir sonraki dakikaya yuvarlanır (review R1-6): gösterilen saat gerçek açılıştan erken olmasın.
        var remainder = lockedUntil.UtcTicks % TimeSpan.TicksPerMinute;
        var shown = remainder == 0 ? lockedUntil : lockedUntil.AddTicks(TimeSpan.TicksPerMinute - remainder);
        return TimeZoneInfo.ConvertTime(shown, TimeZoneInfo.FindSystemTimeZoneById(DisplayTimeZone)).ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private static bool IsSignedIn(Controller controller) => controller.User.Identity?.IsAuthenticated == true;

    private static async Task KeepRenewedSessionAsync(HttpContext http, AuthSessionResponse renewed)
    {
        var current = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
        if (current.Succeeded)
        {
            KonseptCookieEvents.StoreSession(current.Properties!, renewed);
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, current.Principal!, current.Properties).ConfigureAwait(false);
        }
    }

    private IActionResult LogoutFailed(Controller controller, string code)
    {
        var correlation = controller.HttpContext.TraceIdentifier;
        LogLogoutFailed(Logger, code, correlation);
        controller.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        controller.ViewData[ErrorMessageKey] = string.Format(CultureInfo.CurrentCulture, UiText.ServiceUnavailable, correlation);
        return controller.View(ErrorView);
    }

    private bool ExpiresSoon(string? expiresAt) =>
        !DateTimeOffset.TryParse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expires)
        || expires - Time.GetUtcNow() < KonseptCookieEvents.RefreshAhead;

    private static string? ClientIp(Controller controller) => controller.HttpContext.Connection.RemoteIpAddress?.ToString();

    private static string SafeReturnUrl(Controller controller, string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && controller.Url.IsLocalUrl(returnUrl) ? returnUrl : "/";

    private static async Task SignInAsync(Controller controller, AuthSessionResponse session)
    {
        var identity = new ClaimsIdentity(
            [new Claim("sub", session.UserId.ToString()), new Claim(ClaimTypes.Name, session.Name)],
            CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = new AuthenticationProperties { IsPersistent = true };
        KonseptCookieEvents.StoreSession(properties, session);
        await controller.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), properties).ConfigureAwait(false);
    }

    private async Task<IActionResult> CompleteAsync(Controller controller, AuthForm form, string view, Result<AuthSessionResponse> result)
    {
        if (result.IsSuccess)
        {
            await SignInAsync(controller, result.Value).ConfigureAwait(false);
            return controller.LocalRedirect(SafeReturnUrl(controller, form.ReturnUrl));
        }

        form.Password = null;
        ApplyError(controller, form, result.Error!);
        return controller.View(view, form);
    }

    private void ApplyError(Controller controller, AuthForm form, Error error)
    {
        switch (error.Code)
        {
            case ApiClientErrors.ValidationCode:
                foreach (var (field, messages) in error.FieldErrors ?? new Dictionary<string, string[]>())
                {
                    form.Errors[field] = messages.FirstOrDefault() ?? error.Message;
                }

                return;
            case ApiClientErrors.EmailTakenCode:
                form.Errors["Email"] = UiText.EmailTaken;
                return;
            case ApiClientErrors.WrongPasswordCode when error.Metadata?.GetValueOrDefault(ApiClientErrors.RemainingAttemptsKey) is int remaining:
                form.Message = string.Format(CultureInfo.CurrentCulture, UiText.LoginWrongPassword, remaining);
                return;
            case ApiClientErrors.LockedCode when error.Metadata?.GetValueOrDefault(ApiClientErrors.LockedUntilKey) is DateTimeOffset until:
                form.Message = string.Format(CultureInfo.CurrentCulture, UiText.LoginLocked, FormatUnlockTime(until));
                return;
            case ApiClientErrors.InvalidCredentialsCode:
                form.Message = UiText.LoginInvalidCredentials;
                return;
            default:
                if (error.Type == ErrorType.TooManyRequests)
                {
                    form.Message = UiText.TooManyAttempts;
                    return;
                }

                var correlation = controller.HttpContext.TraceIdentifier;
                LogAuthUnavailable(Logger, error.Code, correlation);
                form.Message = string.Format(CultureInfo.CurrentCulture, UiText.ServiceUnavailable, correlation);
                return;
        }
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning, Message = "Kimlik işlemi tamamlanamadı: {Code} (korelasyon {Correlation})")]
    private static partial void LogAuthUnavailable(ILogger logger, string code, string correlation);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Warning, Message = "Api çıkışı başarısız: {Code} (korelasyon {Correlation}); oturum korundu")]
    private static partial void LogLogoutFailed(ILogger logger, string code, string correlation);
}
