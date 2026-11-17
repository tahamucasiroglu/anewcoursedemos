using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.Monitoring.Presentation.Controllers;

/// <summary>
/// Admin paneli Hata bildirimleri (spec 0025): liste, detay, durum değiştirme. Yalnızca <c>admin.panel</c> yetkisiyle (yetki her istekte
/// veritabanındaki rolden, 0011); yetkisiz 403 ve veri yok, kimliksiz 401. Silme ucu yoktur. Rate limit yok (yalnızca admin, LAN).
/// </summary>
[ApiController]
[Route("api/admin/error-reports")]
[Authorize(Policy = AuthPolicies.AdminPanel)]
public sealed class AdminErrorReportsController : ControllerBase
{
    private readonly IAdminErrorReportService _reports;

    /// <summary>Initializes a new instance of the <see cref="AdminErrorReportsController"/> class.</summary>
    /// <param name="reports">Admin hata bildirimi servisi.</param>
    public AdminErrorReportsController(IAdminErrorReportService reports)
    {
        _reports = reports;
    }

    /// <summary>Bildirimlerin bir sayfası (varsayılan açık bildirimler, en yeni üstte, 25'erli).</summary>
    /// <param name="request">Sayfa, kaynak, durum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 · 401 · 403.</returns>
    [HttpGet]
    public Task<IActionResult> List([FromQuery] AdminErrorReportsRequest request, CancellationToken cancellationToken) =>
        _reports.ListAsync(request, cancellationToken).ToActionResult();

    /// <summary>Bir bildirimin detayı.</summary>
    /// <param name="number">Bildirim numarası.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 · 401 · 403 · 404.</returns>
    [HttpGet("{number:long}")]
    public Task<IActionResult> Get(long number, CancellationToken cancellationToken) =>
        _reports.GetAsync(number, cancellationToken).ToActionResult();

    /// <summary>Durumu ve isteğe bağlı admin notunu değiştirir.</summary>
    /// <param name="number">Bildirim numarası.</param>
    /// <param name="request">Durum ve not.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 · 400 · 401 · 403 · 404.</returns>
    [HttpPost("{number:long}/status")]
    public Task<IActionResult> ChangeStatus(long number, [FromBody] ErrorReportStatusRequest? request, CancellationToken cancellationToken) =>
        _reports.ChangeStatusAsync(number, request, cancellationToken).ToActionResult();
}
