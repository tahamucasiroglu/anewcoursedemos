using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Auth;
using Konsept.Web.Resources;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Infrastructure;

/// <summary>
/// API sonuçlarını tarayıcının <c>$.ajax</c> çağrılarına uygun JSON'a çevirir; controller'lar tek satır kalır (K-4).
/// </summary>
public static class BrowserJsonExtensions
{
    /// <summary>
    /// Sistem durumunu her zaman 200 ile döndürür: API'ye ulaşılamazsa <c>apiAvailable=false</c>, böylece
    /// sayfa "API erişilemiyor" durumunu gösterebilir.
    /// </summary>
    /// <param name="resultTask">API çağrısı.</param>
    /// <param name="http">Geçerli istek (oturum token'sız korunduysa 401 "oturum bitti" sayılmaz).</param>
    /// <returns>JSON yanıtı.</returns>
    public static async Task<IActionResult> ToStatusJson(this Task<Result<SystemStatusResponse>> resultTask, HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        var result = WithoutTokenlessUnauthorized(await resultTask.ConfigureAwait(false), http);
        if (!result.IsSuccess && result.Error!.Type == ErrorType.Unauthorized)
        {
            return SessionExpired(); // Oturum Api'de düştü: sessizce "API yok" denmez, tarayıcı girişe yönlenir (AC-13).
        }

        return result.IsSuccess
            ? new JsonResult(new
            {
                apiAvailable = true,
                version = result.Value.Version,
                databaseAvailable = result.Value.DatabaseAvailable,
                agent = result.Value.Agent,
                lastEvent = result.Value.LastEvent,
            })
            : new JsonResult(new { apiAvailable = false });
    }

    /// <summary>Başarıda değeri 200 ile, hatada uygun durum koduyla <c>{ code, message }</c> döndürür.</summary>
    /// <typeparam name="T">Değer tipi.</typeparam>
    /// <param name="resultTask">API çağrısı.</param>
    /// <param name="http">Geçerli istek (oturum token'sız korunduysa 401 "oturum bitti" sayılmaz).</param>
    /// <returns>JSON yanıtı.</returns>
    public static async Task<IActionResult> ToBrowserJson<T>(this Task<Result<T>> resultTask, HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        var result = WithoutTokenlessUnauthorized(await resultTask.ConfigureAwait(false), http);
        if (result.IsSuccess)
        {
            return new JsonResult(result.Value);
        }

        var error = result.Error!;
        return error.Type switch
        {
            ErrorType.Unauthorized => SessionExpired(),
            ErrorType.Forbidden => Forbidden(), // Api yetkiyi reddetti (spec 0011): metin resx'ten, Api'nin gövdesi taşınmaz.
            _ => new JsonResult(new { code = error.Code, message = error.Message }) { StatusCode = StatusOf(error) },
        };
    }

    // Oturum kullanılabilir token olmadan korunduysa (refresh geçici hata + dolmuş token) Api'nin token'sız isteğe 401'i
    // oturumun bittiğini göstermez (review R3-2): "API erişilemiyor" sayılır, yoksa Ajax 401 → /giris → / döngüsü olurdu.
    private static Result<T> WithoutTokenlessUnauthorized<T>(Result<T> result, HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        return !result.IsSuccess && result.Error!.Type == ErrorType.Unauthorized && http.Items.ContainsKey(KonseptCookieEvents.TokenUnavailableItem)
            ? Result<T>.Failure(ApiClientErrors.Unavailable)
            : result;
    }

    private static JsonResult SessionExpired() =>
        new(new { code = KonseptCookieEvents.SessionExpiredCode, message = UiText.SessionExpired }) { StatusCode = StatusCodes.Status401Unauthorized };

    private static JsonResult Forbidden() =>
        new(new { code = WebPolicies.ForbiddenCode, message = UiText.ActionForbidden }) { StatusCode = StatusCodes.Status403Forbidden };

    private static int StatusOf(Error error) => error.Type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Timeout => StatusCodes.Status504GatewayTimeout,
        _ when error.Code == ApiClientErrors.UnavailableCode => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status503ServiceUnavailable,
    };
}
