using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Api.Controllers;

/// <summary>
/// Üretim ayarları uçları: <c>GET|PUT api/production/settings</c> (<see cref="ProductionSettingsDto"/>; doğrulama global filtrede).
/// </summary>
/// <param name="service">Ayar servisi.</param>
[Route("api/production/settings")]
public sealed class ProductionSettingsController(IProductionSettingsService service) : ApiControllerBase
{
    /// <summary>Ayarlar.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ProductionSettingsDto"/>.</returns>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => ToActionResult(await service.GetAsync(ct));

    /// <summary>Ayarları kaydeder.</summary>
    /// <param name="request">Ayarlar.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kaydedilen <see cref="ProductionSettingsDto"/>.</returns>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ProductionSettingsDto request, CancellationToken ct) => ToActionResult(await service.UpdateAsync(request, ct));
}
