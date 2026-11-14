using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.Monitoring.Presentation.Controllers;

/// <summary>
/// Sağlık kontrolü (her zaman kimliksiz, spec 0002 Req 11 istisnası) ve sistem durumu (giriş ister; default-deny).
/// </summary>
[ApiController]
public sealed class SystemController : ControllerBase
{
    private readonly ISystemStatusService _status;

    /// <summary>Initializes a new instance of the <see cref="SystemController"/> class.</summary>
    /// <param name="status">Sistem durumu servisi.</param>
    public SystemController(ISystemStatusService status)
    {
        _status = status;
    }

    /// <summary>API, veritabanı ve sürüm; kullanıcıya ya da ajana ait bilgi içermez.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sağlık bilgisi.</returns>
    [AllowAnonymous]
    [HttpGet("/health")]
    public Task<IActionResult> Health(CancellationToken cancellationToken) =>
        _status.GetHealthAsync(cancellationToken).ToActionResult();

    /// <summary>Site durum alanı: sürüm, veritabanı, ajan ve son Günlük olayı.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sistem durumu.</returns>
    [HttpGet("/api/system/status")]
    public Task<IActionResult> Status(CancellationToken cancellationToken) =>
        _status.GetStatusAsync(cancellationToken).ToActionResult();
}
