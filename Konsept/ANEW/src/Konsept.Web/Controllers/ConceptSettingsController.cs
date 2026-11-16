using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// Konseptler sayfası şeridinin Ajax uçları (spec 0005): seçili profilin model seçimleri, ince ayarı ve kelime denetimi. Giriş
/// ister (default-deny), POST CSRF korumalıdır; sahiplik ve model kuralları Api'dedir (Web yalnızca iletir, K-6).
/// </summary>
public sealed class ConceptSettingsController : Controller
{
    private readonly IKonseptApiClient _api;

    /// <summary>Initializes a new instance of the <see cref="ConceptSettingsController"/> class.</summary>
    /// <param name="api">API istemcisi.</param>
    public ConceptSettingsController(IKonseptApiClient api)
    {
        _api = api;
    }

    /// <summary>Profilin ayarları ve seçilebilir modeller (Ajax).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/konsept-ayarlari/{profileId:guid}")]
    public Task<IActionResult> Get(Guid profileId, CancellationToken cancellationToken) =>
        _api.GetConceptSettingsAsync(profileId, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Profilin ayarlarını kaydeder (Ajax).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="form">Gövde (verilmeyen bölüm değişmez).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konsept-ayarlari/{profileId:guid}")]
    public Task<IActionResult> Save(Guid profileId, [FromBody] SaveConceptSettingsApiRequest? form, CancellationToken cancellationToken) =>
        _api.SaveConceptSettingsAsync(profileId, form, cancellationToken).ToDetailedBrowserJson(HttpContext);
}
