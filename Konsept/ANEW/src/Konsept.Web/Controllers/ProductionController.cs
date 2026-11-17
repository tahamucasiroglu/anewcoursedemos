using System.Text.Json;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// Konseptten Video sayfasının Ajax uçları (spec 0006): profil durumu, üretim ayarları, sıraya alma, Yukarı/Aşağı/Çıkar, Sırayla üret, Durdur,
/// Kaldığı yerden devam et. Giriş ister (default-deny), POST CSRF korumalıdır; kurallar ve sahiplik Api'dedir (Web yalnızca iletir, K-6).
/// </summary>
public sealed class ProductionController : Controller
{
    private readonly IKonseptApiClient _api;

    /// <summary>Initializes a new instance of the <see cref="ProductionController"/> class.</summary>
    /// <param name="api">API istemcisi.</param>
    public ProductionController(IKonseptApiClient api)
    {
        _api = api;
    }

    /// <summary>Profilin durumu (Ajax).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/uretim/{profileId:guid}")]
    public Task<IActionResult> State(Guid profileId, CancellationToken cancellationToken) =>
        _api.GetProductionStateAsync(profileId, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Ayarları kaydeder (Ajax).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="body">Ayarların tamamı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretim/{profileId:guid}/ayarlar")]
    public Task<IActionResult> SaveSettings(Guid profileId, [FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.SaveSettings, profileId, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Üretim sırasına al (Ajax).</summary>
    /// <param name="body">Profil ve konseptler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretim/sira")]
    public Task<IActionResult> Enqueue([FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.Enqueue, null, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Sırayla üret (Ajax).</summary>
    /// <param name="body">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretim/baslat")]
    public Task<IActionResult> Start([FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.Start, null, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Durdur (Ajax).</summary>
    /// <param name="body">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretim/durdur")]
    public Task<IActionResult> Stop([FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.Stop, null, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Yukarı/Aşağı (Ajax).</summary>
    /// <param name="id">İş.</param>
    /// <param name="body">Yön.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretim/is/{id:guid}/tasi")]
    public Task<IActionResult> Move(Guid id, [FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.Move, id, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Sıradan çıkar (Ajax).</summary>
    /// <param name="id">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretim/is/{id:guid}/cikar")]
    public Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.Remove, id, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Kaldığı yerden devam et (Ajax).</summary>
    /// <param name="id">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretim/is/{id:guid}/devam")]
    public Task<IActionResult> Resume(Guid id, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.Resume, id, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>İşin bekleyen indirmesini onayla (spec 0027 Req 5).</summary>
    /// <param name="id">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpPost("/uretim/is/{id:guid}/indir-onayla")]
    public Task<IActionResult> ApproveInstall(Guid id, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.ApproveInstall, id, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>İşin bekleyen indirmesini reddet (spec 0027 Req 6).</summary>
    /// <param name="id">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpPost("/uretim/is/{id:guid}/indir-reddet")]
    public Task<IActionResult> DeclineInstall(Guid id, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.DeclineInstall, id, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Üretilenler: profilin videoları, en yeni üstte (Ajax; spec 0017 Req 20).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/uretim/{profileId:guid}/videolar")]
    public Task<IActionResult> Videos(Guid profileId, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.Videos, profileId, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Videoyu izle: ajanın kısa ömürlü yerel akış adresi (Ajax; spec 0017 Req 21).</summary>
    /// <param name="id">Video.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/uretim/video/{id:guid}/izle")]
    public Task<IActionResult> Stream(Guid id, CancellationToken cancellationToken) =>
        _api.SendProductionAsync(ProductionCommand.Stream, id, null, cancellationToken).ToDetailedBrowserJson(HttpContext);
}
