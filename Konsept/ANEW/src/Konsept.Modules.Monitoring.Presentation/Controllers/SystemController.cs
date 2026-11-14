using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.Monitoring.Presentation.Controllers;

/// <summary>
/// Sağlık kontrolü (her zaman kimliksiz) ve sistem durumu (bu spec'te kimliksiz, 0002'de giriş ister).
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
