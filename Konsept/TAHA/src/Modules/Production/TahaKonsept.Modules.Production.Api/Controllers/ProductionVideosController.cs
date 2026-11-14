using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Api.Controllers;

/// <summary>
/// Üretilen video uçları (<c>api/production/videos</c>): liste (<c>?take=</c>), ayrıntı, sil, klasörde aç, sistem oynatıcısında aç
/// (<c>open-in-player</c>, eş ad <c>open-player</c>), yeni sürüm, yeniden üret.
/// </summary>
/// <param name="service">Video servisi.</param>
[Route("api/production/videos")]
public sealed class ProductionVideosController(IVideoService service) : ApiControllerBase
{
    /// <summary>Videolar.</summary>
    /// <param name="take">Azami adet (varsayılan 100).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="VideoSummaryDto"/> listesi.</returns>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take, CancellationToken ct) => ToActionResult(await service.ListAsync(take, ct));

    /// <summary>Video ayrıntısı.</summary>
    /// <param name="id">Video.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="VideoDetailDto"/>.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => ToActionResult(await service.GetAsync(id, ct));

    /// <summary>Videoyu siler.</summary>
    /// <param name="id">Video.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => ToActionResult(await service.DeleteAsync(id, ct));

    /// <summary>Klasörde açtırır.</summary>
    /// <param name="id">Video.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpPost("{id:guid}/open-folder")]
    public async Task<IActionResult> OpenFolder(Guid id, CancellationToken ct) => ToActionResult(await service.OpenFolderAsync(id, ct));

    /// <summary>Sistem oynatıcısında açtırır.</summary>
    /// <param name="id">Video.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpPost("{id:guid}/open-in-player")]
    [HttpPost("{id:guid}/open-player")]
    public async Task<IActionResult> OpenInPlayer(Guid id, CancellationToken ct) => ToActionResult(await service.OpenInPlayerAsync(id, ct));

    /// <summary>Yeni sürüm (değişmeyen aşamalar yeniden kullanılır).</summary>
    /// <param name="id">Kaynak video.</param>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="QueueItemDto"/>.</returns>
    [HttpPost("{id:guid}/new-version")]
    public async Task<IActionResult> NewVersion(Guid id, [FromBody] NewVersionRequest request, CancellationToken ct) => ToActionResult(await service.NewVersionAsync(id, request, ct));

    /// <summary>Yeniden üret (sıfırdan).</summary>
    /// <param name="id">Kaynak video.</param>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="QueueItemDto"/>.</returns>
    [HttpPost("{id:guid}/regenerate")]
    public async Task<IActionResult> Regenerate(Guid id, [FromBody] RegenerateRequest request, CancellationToken ct) => ToActionResult(await service.RegenerateAsync(id, request, ct));
}
