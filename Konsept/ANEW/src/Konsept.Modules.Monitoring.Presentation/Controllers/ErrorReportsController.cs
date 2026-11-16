using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.Monitoring.Presentation.Controllers;

/// <summary>
/// Hata bildirimleri (spec 0022): web bildirimi ve önizlemesi giriş ister (default-deny; oturumsuz 401); okuma yalnızca
/// <c>admin.panel</c> yetkisiyle (normal kullanıcı kendininkini de okuyamaz, 403). Ajan bildirimi bu controller'da değil, kimlikli ajan
/// hub'ındadır (kimliksiz bildirim ucu yoktur).
/// </summary>
[ApiController]
public sealed class ErrorReportsController : ControllerBase
{
    private readonly IErrorReportService _reports;

    /// <summary>Initializes a new instance of the <see cref="ErrorReportsController"/> class.</summary>
    /// <param name="reports">Hata bildirimi servisi.</param>
    public ErrorReportsController(IErrorReportService reports)
    {
        _reports = reports;
    }

    /// <summary>Modalın gösterdiği otomatik bilgiler (versiyon, zaman, son 20 Günlük kaydı).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Önizleme.</returns>
    [HttpGet("/api/error-reports/preview")]
    public Task<IActionResult> Preview(CancellationToken cancellationToken) =>
        _reports.GetPreviewAsync(cancellationToken).ToActionResult();

    /// <summary>Web'den hata bildirimi gönderir.</summary>
    /// <param name="request">Açıklama, sayfa, korelasyon.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bildirim numarası.</returns>
    [HttpPost("/api/error-reports")]
    public Task<IActionResult> Submit([FromBody] WebErrorReportRequest? request, CancellationToken cancellationToken) =>
        _reports.SubmitWebAsync(request, cancellationToken).ToActionResult();

    /// <summary>Bir hata bildirimini okur (yalnızca admin).</summary>
    /// <param name="number">Bildirim numarası.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bildirim.</returns>
    [HttpGet("/api/error-reports/{number:long}")]
    [Authorize(Policy = AuthPolicies.AdminPanel)]
    public Task<IActionResult> Get(long number, CancellationToken cancellationToken) =>
        _reports.GetAsync(number, cancellationToken).ToActionResult();
}
