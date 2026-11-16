using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.Production.Presentation.Controllers;

/// <summary>
/// Konseptten Video (spec 0006): profil durumu, üretim ayarları, sıraya alma, Yukarı/Aşağı/Çıkar, Sırayla üret/Durdur/Devam. Giriş ister
/// (default-deny); sahiplik servistedir (başkasının profili/işi 404, BR-1). Her uç profilin güncel durumunu döndürür.
/// </summary>
[ApiController]
[Route("api/production")]
public sealed class ProductionController : ControllerBase
{
    private readonly IProductionService _production;

    /// <summary>Initializes a new instance of the <see cref="ProductionController"/> class.</summary>
    /// <param name="production">Servis.</param>
    public ProductionController(IProductionService production)
    {
        _production = production;
    }

    /// <summary>Profilin durumu.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpGet("state")]
    public Task<IActionResult> State([FromQuery] Guid profileId, CancellationToken cancellationToken) =>
        _production.GetAsync(profileId, cancellationToken).ToActionResult();

    /// <summary>Ayarları kaydeder.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="request">Ayarlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpPut("settings")]
    public Task<IActionResult> SaveSettings([FromQuery] Guid profileId, [FromBody] SaveProductionSettingsRequest request, CancellationToken cancellationToken) =>
        _production.SaveSettingsAsync(profileId, request, cancellationToken).ToActionResult();

    /// <summary>Üretim sırasına al.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpPost("queue")]
    public Task<IActionResult> Enqueue([FromBody] EnqueueRequest request, CancellationToken cancellationToken) =>
        _production.EnqueueAsync(request, cancellationToken).ToActionResult();

    /// <summary>Yukarı/Aşağı.</summary>
    /// <param name="id">İş.</param>
    /// <param name="request">Yön.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpPost("jobs/{id:guid}/move")]
    public Task<IActionResult> Move(Guid id, [FromBody] MoveJobRequest request, CancellationToken cancellationToken) =>
        _production.MoveAsync(id, request, cancellationToken).ToActionResult();

    /// <summary>Çıkar (×).</summary>
    /// <param name="id">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpPost("jobs/{id:guid}/remove")]
    public Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken) =>
        _production.RemoveAsync(id, cancellationToken).ToActionResult();

    /// <summary>Kaldığı yerden devam et.</summary>
    /// <param name="id">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpPost("jobs/{id:guid}/resume")]
    public Task<IActionResult> Resume(Guid id, CancellationToken cancellationToken) =>
        _production.ResumeAsync(id, cancellationToken).ToActionResult();

    /// <summary>Sırayla üret.</summary>
    /// <param name="request">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpPost("start")]
    public Task<IActionResult> Start([FromBody] ProfileCommandRequest request, CancellationToken cancellationToken) =>
        _production.StartAsync(request, cancellationToken).ToActionResult();

    /// <summary>Durdur.</summary>
    /// <param name="request">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpPost("stop")]
    public Task<IActionResult> Stop([FromBody] ProfileCommandRequest request, CancellationToken cancellationToken) =>
        _production.StopAsync(request, cancellationToken).ToActionResult();
}
