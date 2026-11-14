using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Models;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Api.Controllers;

/// <summary>
/// İşlem günlüğü uçları: <c>GET api/diagnostics/activity-logs/recent?afterId=&amp;take=</c> (alt şerit),
/// <c>GET api/diagnostics/activity-logs?day=&amp;level=&amp;source=&amp;search=&amp;page=&amp;pageSize=</c> (yönetim tablosu),
/// <c>POST api/diagnostics/activity-logs</c> (Web / ajan istemci olayı).
/// </summary>
[Route("api/diagnostics/activity-logs")]
public sealed class ActivityLogsController : ApiControllerBase
{
    private readonly IActivityLogService _service;

    /// <summary>Oluşturur.</summary>
    /// <param name="service">Günlük servisi.</param>
    public ActivityLogsController(IActivityLogService service)
    {
        _service = service;
    }

    /// <summary>Son kayıtlar (eskiden yeniye).</summary>
    /// <param name="afterId">Son görülen kayıt.</param>
    /// <param name="take">En çok kayıt (varsayılan 50).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><c>ActivityLogDto[]</c>.</returns>
    [HttpGet("recent")]
    public async Task<IActionResult> Recent([FromQuery] Guid? afterId, [FromQuery] int? take, CancellationToken ct) =>
        ToActionResult(await _service.GetRecentAsync(afterId, take, ct));

    /// <summary>Filtreli, sayfalı arama.</summary>
    /// <param name="query">Filtre.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><c>PagedResult&lt;ActivityLogDto&gt;</c>.</returns>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] ActivityLogQuery query, CancellationToken ct) =>
        ToActionResult(await _service.SearchAsync(query, ct));

    /// <summary>İstemci olayını yazar.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>201 + <c>ActivityLogDto</c>.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ActivityLogCreateRequest request, CancellationToken ct) =>
        ToCreatedResult(await _service.CreateAsync(request, ct));
}
