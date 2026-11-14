using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Models;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Api.Controllers;

/// <summary>
/// Hata bildirimi uçları: <c>POST api/diagnostics/error-reports</c> (anonim de olur: oturumsuz kurulum ekranı),
/// <c>GET api/diagnostics/error-reports?status=&amp;page=&amp;pageSize=</c> ve <c>PUT api/diagnostics/error-reports/{id}/status</c> (yönetici).
/// </summary>
[Route("api/diagnostics/error-reports")]
public sealed class ErrorReportsController : ApiControllerBase
{
    private readonly IErrorReportService _service;

    /// <summary>Oluşturur.</summary>
    /// <param name="service">Bildirim servisi.</param>
    public ErrorReportsController(IErrorReportService service)
    {
        _service = service;
    }

    /// <summary>Bildirim gönderir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>201 + <c>ErrorReportDto</c>.</returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] ErrorReportCreateRequest request, CancellationToken ct) =>
        ToCreatedResult(await _service.CreateAsync(request, ct));

    /// <summary>Bildirimleri listeler (yönetici).</summary>
    /// <param name="query">Filtre.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><c>PagedResult&lt;ErrorReportDto&gt;</c>.</returns>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ErrorReportQuery query, CancellationToken ct) =>
        ToActionResult(await _service.ListAsync(query, ct));

    /// <summary>Durum değiştirir (yönetici).</summary>
    /// <param name="id">Bildirim.</param>
    /// <param name="request">Yeni durum.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><c>ErrorReportDto</c>.</returns>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ErrorReportStatusUpdateRequest request, CancellationToken ct) =>
        ToActionResult(await _service.ChangeStatusAsync(id, request, ct));
}
