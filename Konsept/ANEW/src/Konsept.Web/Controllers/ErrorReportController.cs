using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// "Hata bildir" modalının Ajax uçları (spec 0022 Req 1–3, 13). Giriş ister (default-deny; oturumsuz istek girişe gider), POST CSRF
/// korumalıdır; kurallar (doğrulama, sınır, bağlam) Api'dedir, Web yalnızca iletir.
/// </summary>
public sealed class ErrorReportController : Controller
{
    private readonly IKonseptApiClient _api;

    /// <summary>Initializes a new instance of the <see cref="ErrorReportController"/> class.</summary>
    /// <param name="api">API istemcisi.</param>
    public ErrorReportController(IKonseptApiClient api)
    {
        _api = api;
    }

    /// <summary>Otomatik eklenecek bilgiler (Ajax).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/hata-bildir/onizleme")]
    public Task<IActionResult> Preview(CancellationToken cancellationToken) =>
        _api.GetErrorReportPreviewAsync(cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Hata bildirimi gönderir (Ajax).</summary>
    /// <param name="form">Açıklama, sayfa, korelasyon.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/hata-bildir")]
    public Task<IActionResult> Submit([FromBody] ErrorReportApiRequest? form, CancellationToken cancellationToken) =>
        _api.SubmitErrorReportAsync(form, cancellationToken).ToDetailedBrowserJson(HttpContext);
}
