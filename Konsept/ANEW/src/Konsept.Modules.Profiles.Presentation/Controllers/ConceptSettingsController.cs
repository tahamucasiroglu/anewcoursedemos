using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Application.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.Profiles.Presentation.Controllers;

/// <summary>
/// Bir profilin Konseptler sayfası ayarları (spec 0005): model seçimleri, ince ayar, kelime denetimi. Başka kullanıcının profili
/// 404 "Profil bulunamadı" (BR-1); seçilemeyen model 400 (BR-13). Giriş ister (default-deny).
/// </summary>
[ApiController]
[Route("api/profiles/{id:guid}/concept-settings")]
public sealed class ConceptSettingsController : ControllerBase
{
    private readonly IConceptSettingsService _settings;

    /// <summary>Initializes a new instance of the <see cref="ConceptSettingsController"/> class.</summary>
    /// <param name="settings">Konsept ayarları servisi.</param>
    public ConceptSettingsController(IConceptSettingsService settings)
    {
        _settings = settings;
    }

    /// <summary>Ayarlar ve seçilebilir metin modelleri.</summary>
    /// <param name="id">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 · 404 · 401.</returns>
    [HttpGet]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        _settings.GetAsync(id, cancellationToken).ToActionResult();

    /// <summary>Ayarları kaydeder (verilmeyen bölüm değişmez).</summary>
    /// <param name="id">Profil.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 · 400 · 404 · 401.</returns>
    [HttpPut]
    public Task<IActionResult> Save(Guid id, [FromBody] SaveConceptSettingsRequest request, CancellationToken cancellationToken) =>
        _settings.SaveAsync(id, request, cancellationToken).ToActionResult();
}
