using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Api.Controllers;

/// <summary>
/// Üretim işi uçları: <c>GET api/production/jobs/{id}</c> (<see cref="JobDetailDto"/>: tüm aşama çalıştırmaları, hash'ler, maliyet, süreler).
/// </summary>
/// <param name="service">İş sorgu servisi.</param>
[Route("api/production/jobs")]
public sealed class ProductionJobsController(IJobQueryService service) : ApiControllerBase
{
    /// <summary>İş ayrıntısı.</summary>
    /// <param name="id">İş.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="JobDetailDto"/>.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => ToActionResult(await service.GetAsync(id, ct));
}
