using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Api.Controllers;

/// <summary>
/// Üretim sırası uçları (<c>api/production/queue</c>): durum (GET, web 2 sn'de sorgular), ekle (POST), taşı (<c>{id}/move</c>),
/// çıkar (DELETE <c>{id}</c>; çalışıyorsa iptal), sırayla üret / durdur / devam (<c>start|pause|resume</c>), öğe durumu (<c>{id}/status</c>),
/// bitenleri temizle (<c>clear-finished</c>).
/// </summary>
/// <param name="service">Sıra servisi.</param>
[Route("api/production/queue")]
public sealed class ProductionQueueController(IProductionQueueService service) : ApiControllerBase
{
    /// <summary>Sıranın tamamı.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="QueueStateDto"/>.</returns>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => ToActionResult(await service.GetStateAsync(ct));

    /// <summary>Konseptleri sıraya ekler.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="QueueStateDto"/>.</returns>
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddToQueueRequest request, CancellationToken ct) => ToActionResult(await service.AddAsync(request, ct));

    /// <summary>Öğeyi yukarı/aşağı taşır.</summary>
    /// <param name="id">Öğe.</param>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="QueueStateDto"/>.</returns>
    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveQueueItemRequest request, CancellationToken ct) => ToActionResult(await service.MoveAsync(id, request, ct));

    /// <summary>Öğeyi sıradan çıkarır.</summary>
    /// <param name="id">Öğe.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct) => ToActionResult(await service.RemoveAsync(id, ct));

    /// <summary>Sırayla üretimi başlatır.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="QueueStateDto"/>.</returns>
    [HttpPost("start")]
    public async Task<IActionResult> Start(CancellationToken ct) => ToActionResult(await service.StartAsync(ct));

    /// <summary>Durdurur.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="QueueStateDto"/>.</returns>
    [HttpPost("pause")]
    public async Task<IActionResult> Pause(CancellationToken ct) => ToActionResult(await service.PauseAsync(ct));

    /// <summary>Kaldığı yerden sürdürür.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="QueueStateDto"/>.</returns>
    [HttpPost("resume")]
    public async Task<IActionResult> Resume(CancellationToken ct) => ToActionResult(await service.ResumeAsync(ct));

    /// <summary>Tek öğenin durumu.</summary>
    /// <param name="id">Öğe.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="QueueItemDto"/>.</returns>
    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, CancellationToken ct) => ToActionResult(await service.GetItemAsync(id, ct));

    /// <summary>Biten/hatalı/iptal öğeleri kaldırır.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="QueueStateDto"/>.</returns>
    [HttpPost("clear-finished")]
    public async Task<IActionResult> ClearFinished(CancellationToken ct) => ToActionResult(await service.ClearFinishedAsync(ct));
}
