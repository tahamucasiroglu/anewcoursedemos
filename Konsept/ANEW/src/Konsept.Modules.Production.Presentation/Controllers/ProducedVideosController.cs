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
    private readonly IVideoVersionService _versions;

    /// <summary>Initializes a new instance of the <see cref="ProducedVideosController"/> class.</summary>
    /// <param name="videos">Servis.</param>
    /// <param name="versions">Yeni sürüm ve yeniden üret servisi (spec 0020).</param>
    public ProducedVideosController(IProducedVideoService videos, IVideoVersionService versions)
    {
        _videos = videos;
        _versions = versions;
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

    /// <summary>Yeni sürüm alanı: taban ayarları ve seçenekler (spec 0020).</summary>
    /// <param name="id">Video (konsept).</param>
    /// <param name="baseVersionId">Taban sürüm.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Seçenekler.</returns>
    [HttpGet("{id:guid}/version-options")]
    public Task<IActionResult> VersionOptions(Guid id, [FromQuery] Guid? baseVersionId, CancellationToken cancellationToken) =>
        _versions.GetOptionsAsync(id, baseVersionId, cancellationToken).ToActionResult();

    /// <summary>Yeni sürüm önizlemesi (spec 0020 Req 3).</summary>
    /// <param name="id">Video.</param>
    /// <param name="request">Yeni ayarlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Önizleme.</returns>
    [HttpPost("{id:guid}/version-preview")]
    public Task<IActionResult> VersionPreview(Guid id, [FromBody] NewVersionRequest request, CancellationToken cancellationToken) =>
        _versions.PreviewAsync(id, request, cancellationToken).ToActionResult();

    /// <summary>Yeni sürümü başlat (spec 0020 Req 1, 6, 8).</summary>
    /// <param name="id">Video.</param>
    /// <param name="request">Yeni ayarlar ve online onayı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sürüm işi.</returns>
    [HttpPost("{id:guid}/versions")]
    public Task<IActionResult> StartVersion(Guid id, [FromBody] NewVersionRequest request, CancellationToken cancellationToken) =>
        _versions.StartAsync(id, request, cancellationToken).ToActionResult();

    /// <summary>Yeniden üret bağlamını aç (spec 0020 Req 4).</summary>
    /// <param name="id">Video.</param>
    /// <param name="baseVersionId">Taban sürüm.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bağlam.</returns>
    [HttpPost("{id:guid}/regenerate/open")]
    public Task<IActionResult> OpenRegeneration(Guid id, [FromQuery] Guid? baseVersionId, CancellationToken cancellationToken) =>
        _versions.OpenRegenerationAsync(id, baseVersionId, cancellationToken).ToActionResult();

    /// <summary>Yeniden üretim önizlemesi (spec 0020 Req 3, 5).</summary>
    /// <param name="id">Video.</param>
    /// <param name="token">Bağlam belirteci.</param>
    /// <param name="baseVersionId">Taban sürüm.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Önizleme.</returns>
    [HttpGet("{id:guid}/regenerate")]
    public Task<IActionResult> RegenerationPreview(Guid id, [FromQuery] string? token, [FromQuery] Guid? baseVersionId, CancellationToken cancellationToken) =>
        _versions.PreviewRegenerationAsync(id, token, baseVersionId, cancellationToken).ToActionResult();

    /// <summary>Yeniden üretimi başlat (spec 0020 Req 4–6).</summary>
    /// <param name="id">Video.</param>
    /// <param name="request">Bağlam, "Yine de baştan üret", online onayı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sürüm işi.</returns>
    [HttpPost("{id:guid}/regenerate")]
    public Task<IActionResult> StartRegeneration(Guid id, [FromBody] RegenerateVersionRequest request, CancellationToken cancellationToken) =>
        _versions.StartRegenerationAsync(id, request, cancellationToken).ToActionResult();

    /// <summary>Tekrar dene (spec 0020 Req 10).</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sürüm işi.</returns>
    [HttpPost("{id:guid}/retry")]
    public Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken) =>
        _versions.RetryAsync(id, cancellationToken).ToActionResult();

    /// <summary>Sürümü kaldır (spec 0020 Req 10).</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kaldırılan iş.</returns>
    [HttpPost("{id:guid}/remove-failed")]
    public Task<IActionResult> RemoveFailed(Guid id, CancellationToken cancellationToken) =>
        _versions.RemoveFailedAsync(id, cancellationToken).ToActionResult();

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
