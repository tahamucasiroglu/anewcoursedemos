using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Api.Controllers;

/// <summary>
/// Okunuş sözlüğü uçları (<c>api/production/pronunciations</c>): liste (GET), ekle/güncelle/onayla (POST), sil (DELETE <c>{id}</c>),
/// önizleme (<c>POST preview</c> → normalize + ses adresi/base64).
/// </summary>
/// <param name="service">Sözlük servisi.</param>
[Route("api/production/pronunciations")]
public sealed class PronunciationsController(IPronunciationService service) : ApiControllerBase
{
    /// <summary>Kayıtlar.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="PronunciationDto"/> listesi.</returns>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => ToActionResult(await service.ListAsync(ct));

    /// <summary>Ekler/günceller.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="PronunciationDto"/>.</returns>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SavePronunciationRequest request, CancellationToken ct) => ToActionResult(await service.SaveAsync(request, ct));

    /// <summary>Siler.</summary>
    /// <param name="id">Kayıt.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => ToActionResult(await service.DeleteAsync(id, ct));

    /// <summary>Önizleme.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="PronunciationPreviewResponse"/>.</returns>
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] PronunciationPreviewRequest request, CancellationToken ct) => ToActionResult(await service.PreviewAsync(request, ct));
}
