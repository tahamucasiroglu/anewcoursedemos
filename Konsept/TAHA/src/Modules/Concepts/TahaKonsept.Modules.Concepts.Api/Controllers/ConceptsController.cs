using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Api.Controllers;

/// <summary>
/// Konsept kayıtları uçları (oturumdaki kullanıcı): <c>GET api/concepts?status=</c>, <c>GET api/concepts/deleted</c>,
/// <c>GET|PUT|DELETE api/concepts/{id}</c>, <c>POST api/concepts/{id}/approve</c>, <c>POST api/concepts/{id}/restore</c>,
/// <c>DELETE api/concepts/{id}/purge</c>. Doğrulama global filtrede.
/// </summary>
/// <param name="service">Konsept servisi.</param>
[Route("api/concepts")]
public sealed class ConceptsController(IConceptService service) : ApiControllerBase
{
    /// <summary>Konseptler (durum verilmezse silinmemiş hepsi).</summary>
    /// <param name="status">Durum.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ConceptSummaryDto"/> listesi.</returns>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ConceptStatus? status, CancellationToken ct) => ToActionResult(await service.ListAsync(status, ct));

    /// <summary>Silinen konseptler.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ConceptSummaryDto"/> listesi.</returns>
    [HttpGet("deleted")]
    public async Task<IActionResult> Deleted(CancellationToken ct) => ToActionResult(await service.ListDeletedAsync(ct));

    /// <summary>Konsept ayrıntısı.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ConceptDetailDto"/>.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => ToActionResult(await service.GetAsync(id, ct));

    /// <summary>Belgeyi doğrudan düzenler.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ConceptDetailDto"/>.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateConceptRequest request, CancellationToken ct) =>
        ToActionResult(await service.UpdateAsync(id, request, ct));

    /// <summary>Silinenler'e taşır.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => ToActionResult(await service.DeleteAsync(id, ct));

    /// <summary>Onaylar.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ConceptDetailDto"/>.</returns>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct) => ToActionResult(await service.ApproveAsync(id, ct));

    /// <summary>Silinenler'den geri alır.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ConceptDetailDto"/>.</returns>
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct) => ToActionResult(await service.RestoreAsync(id, ct));

    /// <summary>Kalıcı olarak siler.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpDelete("{id:guid}/purge")]
    public async Task<IActionResult> Purge(Guid id, CancellationToken ct) => ToActionResult(await service.PurgeAsync(id, ct));
}
