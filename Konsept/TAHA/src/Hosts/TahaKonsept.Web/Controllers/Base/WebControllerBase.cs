using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Infrastructure.Api;
using TahaKonsept.Web.Infrastructure.Api.Abstract;

namespace TahaKonsept.Web.Controllers.Base;

/// <summary>
/// Tüm MVC controller'larının tabanı. Ajax uçları tek satırdır:
/// <c>=&gt; ProxyAsync(c =&gt; c.PostAsync&lt;XRequest, XResponse&gt;("api/...", r, ct));</c>
/// Önemli üyeler: <see cref="ProxyAsync{T}"/> (API çağrısını yapıp sonucu tarayıcıya aynen aktarır),
/// <see cref="ToActionResult{T}"/> (<see cref="ApiCallResult{T}"/> → IActionResult; hata gövdesi ProblemDetails olarak
/// korunur), <see cref="ApiUrl"/> (boş sorgu parametrelerini atlayarak adres kurar), <see cref="Seg"/> (yol parçası kodlama).
/// </summary>
public abstract class WebControllerBase : Controller
{
    private IApiClient? _api;

    /// <summary>İstek kapsamındaki API istemcisi (DI'dan tembel alınır; kurucu parametresi gerektirmez).</summary>
    protected IApiClient Api => _api ??= HttpContext.RequestServices.GetRequiredService<IApiClient>();

    /// <summary>API çağrısını yapar ve sonucu tarayıcıya aktarır.</summary>
    /// <typeparam name="T">Cevap türü (bilinmeyen sözleşmelerde <see cref="JsonElement"/>).</typeparam>
    /// <param name="call">API istemcisiyle yapılacak çağrı.</param>
    protected async Task<IActionResult> ProxyAsync<T>(Func<IApiClient, Task<ApiCallResult<T>>> call) =>
        ToActionResult(await call(Api));

    /// <summary>
    /// API sonucunu MVC sonucuna çevirir: başarıda 200 + veri (veri yoksa 204), hatada aynı durum kodu ve API'nin
    /// ProblemDetails gövdesi (JSON değilse Türkçe mesajlı yeni bir ProblemDetails).
    /// </summary>
    /// <typeparam name="T">Veri türü.</typeparam>
    /// <param name="result">API sonucu.</param>
    protected IActionResult ToActionResult<T>(ApiCallResult<T> result)
    {
        if (result.IsSuccess)
        {
            return result.Data is null || result.Data is JsonElement { ValueKind: JsonValueKind.Undefined or JsonValueKind.Null }
                ? NoContent()
                : Ok(result.Data);
        }

        var status = result.StatusCode is >= 400 and < 600 ? result.StatusCode : StatusCodes.Status502BadGateway;
        if (!string.IsNullOrWhiteSpace(result.ErrorBody) && LooksLikeJson(result.ErrorBody, result.ErrorContentType))
        {
            return new ContentResult
            {
                StatusCode = status,
                Content = result.ErrorBody,
                ContentType = "application/problem+json; charset=utf-8",
            };
        }

        return Problem(title: result.ErrorMessage ?? "İşlem başarısız oldu.", statusCode: status);
    }

    /// <summary>
    /// Göreli API adresi kurar; değeri null/boş olan sorgu parametrelerini atlar ve değerleri kodlar.
    /// Ör. <c>ApiUrl("api/x", ("afterId", id))</c>.
    /// </summary>
    /// <param name="path">Göreli yol.</param>
    /// <param name="query">Sorgu parametreleri.</param>
    protected static string ApiUrl(string path, params (string Key, object? Value)[] query)
    {
        var builder = new StringBuilder(path);
        var separator = path.Contains('?') ? '&' : '?';
        foreach (var (key, value) in query)
        {
            var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            builder.Append(separator).Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(text));
            separator = '&';
        }

        return builder.ToString();
    }

    /// <summary>Yol parçası olarak güvenli kodlama (ör. kimlikler).</summary>
    /// <param name="value">Değer.</param>
    protected static string Seg(string value) => Uri.EscapeDataString(value);

    private static bool LooksLikeJson(string body, string? contentType) =>
        (contentType?.Contains("json", StringComparison.OrdinalIgnoreCase) ?? false) || body.TrimStart().StartsWith('{');
}
