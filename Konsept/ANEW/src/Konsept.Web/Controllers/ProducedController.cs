using System.Text.Json;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// Üretilen Videolar sayfasının Ajax uçları (spec 0007): profilin videoları (50'şerli), seçilen videonun sürümleri, Klasörde aç / Sistem
/// oynatıcısında aç; Sil ve Silinenler'in Videolar sekmesi (liste, Kalıcı sil — spec 0019). Giriş ister (default-deny), POST CSRF korumalıdır; kurallar ve sahiplik Api'dedir (Web yalnızca iletir, K-6). Akış
/// 0017'nin <c>/uretim/video/{id}/izle</c> ucudur.
/// </summary>
public sealed class ProducedController : Controller
{
    private readonly IKonseptApiClient _api;

    /// <summary>Initializes a new instance of the <see cref="ProducedController"/> class.</summary>
    /// <param name="api">API istemcisi.</param>
    public ProducedController(IKonseptApiClient api)
    {
        _api = api;
    }

    /// <summary>Profilin videoları (Ajax).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="atla">Atlanacak öğe.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/uretilenler/{profileId:guid}/liste")]
    public Task<IActionResult> List(Guid profileId, [FromQuery] int atla, CancellationToken cancellationToken) =>
        _api.ListProducedAsync(profileId, atla, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Videonun sürümleri (Ajax).</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/uretilenler/video/{id:guid}")]
    public Task<IActionResult> Video(Guid id, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.ProducedVideo, id, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Klasörde aç / Sistem oynatıcısında aç (Ajax).</summary>
    /// <param name="id">Sürüm.</param>
    /// <param name="body">Eylem (<c>{"action":"folder"|"player"}</c>).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretilenler/surum/{id:guid}/ac")]
    public Task<IActionResult> Open(Guid id, [FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.OpenFile, id, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Sil: videoyu tüm sürümleriyle Silinenler'e gönderir (Ajax; spec 0019).</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretilenler/video/{id:guid}/sil")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.DeleteVideo, id, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Yeni sürüm alanı: taban ayarları ve seçenekler (Ajax; spec 0020).</summary>
    /// <param name="id">Video.</param>
    /// <param name="taban">Taban sürüm.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/uretilenler/video/{id:guid}/yeni-surum")]
    public Task<IActionResult> VersionOptions(Guid id, [FromQuery] Guid? taban, CancellationToken cancellationToken) =>
        _api.SendVideoVersionQueryAsync(id, VideoVersionQuery.Options, taban, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Yeni sürüm önizlemesi (Ajax; spec 0020).</summary>
    /// <param name="id">Video.</param>
    /// <param name="body">Yeni ayarlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretilenler/video/{id:guid}/yeni-surum/onizle")]
    public Task<IActionResult> VersionPreview(Guid id, [FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.VersionPreview, id, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Yeni sürümü başlat (Ajax; spec 0020).</summary>
    /// <param name="id">Video.</param>
    /// <param name="body">Yeni ayarlar ve online onayı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretilenler/video/{id:guid}/yeni-surum")]
    public Task<IActionResult> StartVersion(Guid id, [FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.StartVersion, id, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Yeniden üret bağlamını aç (Ajax; spec 0020).</summary>
    /// <param name="id">Video.</param>
    /// <param name="taban">Taban sürüm.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretilenler/video/{id:guid}/yeniden-uret/ac")]
    public Task<IActionResult> OpenRegeneration(Guid id, [FromQuery] Guid? taban, CancellationToken cancellationToken) =>
        _api.SendVideoVersionQueryAsync(id, VideoVersionQuery.OpenRegeneration, taban, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Yeniden üretim önizlemesi (Ajax; spec 0020).</summary>
    /// <param name="id">Video.</param>
    /// <param name="belirtec">Bağlam belirteci.</param>
    /// <param name="taban">Taban sürüm.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/uretilenler/video/{id:guid}/yeniden-uret")]
    public Task<IActionResult> RegenerationPreview(Guid id, [FromQuery] string? belirtec, [FromQuery] Guid? taban, CancellationToken cancellationToken) =>
        _api.SendVideoVersionQueryAsync(id, VideoVersionQuery.RegenerationPreview, taban, belirtec, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Yeniden üretimi başlat (Ajax; spec 0020).</summary>
    /// <param name="id">Video.</param>
    /// <param name="body">Belirteç, taban, "Yine de baştan üret", online onayı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretilenler/video/{id:guid}/yeniden-uret")]
    public Task<IActionResult> StartRegeneration(Guid id, [FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.StartRegeneration, id, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Tekrar dene (Ajax; spec 0020).</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretilenler/video/{id:guid}/tekrar-dene")]
    public Task<IActionResult> RetryVersion(Guid id, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.RetryVersion, id, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Sürümü kaldır (Ajax; spec 0020).</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretilenler/video/{id:guid}/surumu-kaldir")]
    public Task<IActionResult> RemoveFailedVersion(Guid id, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.RemoveFailedVersion, id, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Silinenler — Videolar (Ajax; spec 0019).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="sayfa">Sayfa.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/silinenler/videolar/{profileId:guid}")]
    public Task<IActionResult> Deleted(Guid profileId, [FromQuery] int sayfa, CancellationToken cancellationToken) =>
        _api.ListDeletedVideosAsync(profileId, sayfa, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Kalıcı sil (Ajax; spec 0019).</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/silinenler/videolar/{id:guid}/kalici-sil")]
    public Task<IActionResult> Purge(Guid id, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.PurgeVideo, id, null, cancellationToken).ToDetailedBrowserJson(HttpContext);
}
