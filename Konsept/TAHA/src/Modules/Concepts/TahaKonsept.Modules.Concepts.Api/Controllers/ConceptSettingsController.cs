using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Api.Controllers;

/// <summary>
/// Konsept üretim ayarları: <c>GET|PUT api/concepts/settings</c> (modeller + efor, kelime denetimi, ince ayar, gerçek bilgi).
/// </summary>
/// <param name="service">Ayar servisi.</param>
[Route("api/concepts/settings")]
public sealed class ConceptSettingsController(IConceptSettingsService service) : ApiControllerBase
{
    /// <summary>Ayarları getirir.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ConceptSettingsDto"/>.</returns>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => ToActionResult(await service.GetAsync(ct));

    /// <summary>Ayarları kaydeder.</summary>
    /// <param name="settings">Ayarlar.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ConceptSettingsDto"/>.</returns>
    [HttpPut]
    public async Task<IActionResult> Save([FromBody] ConceptSettingsDto settings, CancellationToken ct) => ToActionResult(await service.SaveAsync(settings, ct));
}
