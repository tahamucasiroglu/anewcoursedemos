using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.Monitoring.Presentation.Controllers;

/// <summary>
/// Admin paneli İstatistikler ve Aktiviteler (spec 0010 Req 1, 3–9). Yalnızca <c>admin.panel</c> yetkisiyle açılır (yetki her
/// istekte veritabanındaki rolden kurulur, 0011); yetkisiz 403 ve veri yok, kimliksiz 401. Rate limit yok (yalnızca admin, LAN).
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = AuthPolicies.AdminPanel)]
public sealed class AdminStatisticsController : ControllerBase
{
    private readonly IAdminStatisticsService _statistics;

    /// <summary>Initializes a new instance of the <see cref="AdminStatisticsController"/> class.</summary>
    /// <param name="statistics">Admin istatistik servisi.</param>
    public AdminStatisticsController(IAdminStatisticsService statistics)
    {
        _statistics = statistics;
    }

    /// <summary>Özet sayılar ve günlük seri.</summary>
    /// <param name="days">Aralık: 7 / 30 / 90 (varsayılan 30).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 · 401 · 403.</returns>
    [HttpGet("statistics")]
    public Task<IActionResult> Statistics([FromQuery] int? days, CancellationToken cancellationToken) =>
        _statistics.GetStatisticsAsync(days, cancellationToken).ToActionResult();

    /// <summary>Aktivitelerin bir sayfası (en yeni üstte, 25'erli).</summary>
    /// <param name="request">Sayfa, kullanıcı araması, tarih aralığı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 · 401 · 403.</returns>
    [HttpGet("activities")]
    public Task<IActionResult> Activities([FromQuery] AdminActivitiesRequest request, CancellationToken cancellationToken) =>
        _statistics.GetActivitiesAsync(request, cancellationToken).ToActionResult();
}
