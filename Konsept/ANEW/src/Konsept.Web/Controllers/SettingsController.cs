using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.Infrastructure;
using Konsept.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// Ayarlar (spec 0004 Req 14): sayfa ve Ajax uçları ("API key'ler" ve "Modeller" bölümleri). Giriş ister (default-deny); key'ler
/// Api'de kullanıcıya göre süzülür, Web hiçbir key saklamaz ve tam key görmez (yalnızca kaydetme isteğini iletir).
/// </summary>
public sealed class SettingsController : Controller
{
    private readonly IKonseptApiClient _api;

    /// <summary>Initializes a new instance of the <see cref="SettingsController"/> class.</summary>
    /// <param name="api">API istemcisi.</param>
    public SettingsController(IKonseptApiClient api)
    {
        _api = api;
    }

    /// <summary>Ayarlar sayfası (bölümler settings.js ile yüklenir).</summary>
    /// <returns>Sayfa.</returns>
    [HttpGet("/ayarlar")]
    public IActionResult Index() => View();

    /// <summary>Model kataloğu (Ajax).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/ayarlar/modeller")]
    public Task<IActionResult> Models(CancellationToken cancellationToken) =>
        _api.GetCatalogAsync(cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Sağlayıcı satırları (Ajax; yalnızca maskeli key'ler).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/ayarlar/api-keyler")]
    public Task<IActionResult> Keys(CancellationToken cancellationToken) =>
        _api.GetApiKeysAsync(cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Key'i kaydeder (Ajax, CSRF korumalı).</summary>
    /// <param name="provider">Sağlayıcı kimliği.</param>
    /// <param name="form">Key formu.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/ayarlar/api-keyler/{provider}")]
    public Task<IActionResult> Save(string provider, [FromBody] ApiKeyForm? form, CancellationToken cancellationToken) =>
        _api.SaveApiKeyAsync(provider, form?.ApiKey, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Key'i siler (Ajax, CSRF korumalı).</summary>
    /// <param name="provider">Sağlayıcı kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/ayarlar/api-keyler/{provider}/sil")]
    public Task<IActionResult> Delete(string provider, CancellationToken cancellationToken) =>
        _api.DeleteApiKeyAsync(provider, cancellationToken).ToDetailedBrowserJson(HttpContext);
}
