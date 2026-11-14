using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Dtos;
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
    /// <returns>JSON yanıtı.</returns>
    public static async Task<IActionResult> ToStatusJson(this Task<Result<SystemStatusResponse>> resultTask)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        var result = await resultTask.ConfigureAwait(false);
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
    /// <returns>JSON yanıtı.</returns>
    public static async Task<IActionResult> ToBrowserJson<T>(this Task<Result<T>> resultTask)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return new JsonResult(result.Value);
        }

        var error = result.Error!;
        return new JsonResult(new { code = error.Code, message = error.Message }) { StatusCode = StatusOf(error) };
    }

    private static int StatusOf(Error error) => error.Type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Timeout => StatusCodes.Status504GatewayTimeout,
        _ when error.Code == ApiClientErrors.UnavailableCode => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status503ServiceUnavailable,
    };
}
