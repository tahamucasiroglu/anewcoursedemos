using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Infrastructure;
using Konsept.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// Üst bar profil seçimi ve profil modalının Ajax uçları (spec 0003). Giriş ister (default-deny), POST'lar CSRF korumalıdır;
/// sahiplik ve kurallar Api'dedir (Web yalnızca iletir). Sistem talimatı hiçbir uçta yoktur.
/// </summary>
public sealed class ProfilesController : Controller
{
    private readonly IKonseptApiClient _api;

    /// <summary>Initializes a new instance of the <see cref="ProfilesController"/> class.</summary>
    /// <param name="api">API istemcisi.</param>
    public ProfilesController(IKonseptApiClient api)
    {
        _api = api;
    }

    /// <summary>Profiller, seçili profil, sınır ve şablonlar (Ajax).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/profiller")]
    public Task<IActionResult> List(CancellationToken cancellationToken) =>
        _api.GetProfilesAsync(cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Profil oluşturur (Ajax).</summary>
    /// <param name="form">Alanlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/profiller")]
    public Task<IActionResult> Create([FromBody] SaveProfileApiRequest? form, CancellationToken cancellationToken) =>
        _api.CreateProfileAsync(form, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Seçili profili değiştirir (Ajax).</summary>
    /// <param name="form">Profil kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/profiller/secili")]
    public Task<IActionResult> Select([FromBody] SelectProfileForm? form, CancellationToken cancellationToken) =>
        _api.SelectProfileAsync(form?.ProfileId, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Profili düzenler (Ajax).</summary>
    /// <param name="id">Profil.</param>
    /// <param name="form">Alanlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/profiller/{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveProfileApiRequest? form, CancellationToken cancellationToken) =>
        _api.UpdateProfileAsync(id, form, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Profili kalıcı siler (Ajax).</summary>
    /// <param name="id">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/profiller/{id:guid}/sil")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        _api.DeleteProfileAsync(id, cancellationToken).ToDetailedBrowserJson(HttpContext);
}
