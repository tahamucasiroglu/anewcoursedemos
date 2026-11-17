using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.Production.Presentation.Controllers;

/// <summary>
/// Üretilen Videolar (spec 0007): profilin videoları (50'şerli), seçilen videonun sürümleri, dosya eylemleri. Giriş ister (default-deny);
/// sahiplik servistedir (başkasınınki 404, ajana komut gitmez — BR-1). Akış 0017'nin <c>POST api/production/videos/{id}/stream</c> ucudur.
/// </summary>
[ApiController]
[Route("api/produced")]
public sealed class ProducedVideosController : ControllerBase
{
    private readonly IProducedVideoService _videos;

    /// <summary>Initializes a new instance of the <see cref="ProducedVideosController"/> class.</summary>
    /// <param name="videos">Servis.</param>
    public ProducedVideosController(IProducedVideoService videos)
    {
        _videos = videos;
    }

    /// <summary>Profilin videoları.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="skip">Atlanacak öğe.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Liste parçası.</returns>
    [HttpGet]
    public Task<IActionResult> List([FromQuery] Guid profileId, [FromQuery] int skip, CancellationToken cancellationToken) =>
        _videos.ListAsync(profileId, skip, cancellationToken).ToActionResult();

    /// <summary>Videonun sürümleri.</summary>
    /// <param name="id">Video (konsept).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayrıntı.</returns>
    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        _videos.GetAsync(id, cancellationToken).ToActionResult();

    /// <summary>Klasörde aç / Sistem oynatıcısında aç.</summary>
    /// <param name="id">Sürüm.</param>
    /// <param name="request">Eylem.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    [HttpPost("versions/{id:guid}/open")]
    public Task<IActionResult> Open(Guid id, [FromBody] FileActionRequest request, CancellationToken cancellationToken) =>
        _videos.OpenFileAsync(id, request, cancellationToken).ToActionResult();

    /// <summary>Sil (spec 0019): videoyu tüm sürümleriyle Silinenler'e gönderir.</summary>
    /// <param name="id">Video (konsept).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinenler satırı.</returns>
    [HttpPost("{id:guid}/delete")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        _videos.DeleteAsync(id, cancellationToken).ToActionResult();

    /// <summary>Silinenler — Videolar (spec 0019).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="page">Sayfa.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Liste.</returns>
    [HttpGet("deleted")]
    public Task<IActionResult> Deleted([FromQuery] Guid profileId, [FromQuery] int page, CancellationToken cancellationToken) =>
        _videos.ListDeletedAsync(profileId, page, cancellationToken).ToActionResult();

    /// <summary>Kalıcı sil (spec 0019).</summary>
    /// <param name="id">Video (konsept).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kaldırılan satır.</returns>
    [HttpPost("deleted/{id:guid}/purge")]
    public Task<IActionResult> Purge(Guid id, CancellationToken cancellationToken) =>
        _videos.PurgeAsync(id, cancellationToken).ToActionResult();
}
