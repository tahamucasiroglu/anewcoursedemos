using Konsept.Modules.Profiles.Application.Abstract;
using Konsept.Modules.Profiles.Application.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.Profiles.Presentation.Controllers;

/// <summary>
/// İsteği yapan kullanıcının profilleri (spec 0003, H-1). Her işlem oturumdaki kullanıcınındır; başka kullanıcının profil kimliği
/// 404 "Profil bulunamadı" döner (AC-15). Sistem talimatı hiçbir uçta dönmez (Req 16). Giriş ister (default-deny).
/// </summary>
[ApiController]
[Route("api/profiles")]
public sealed class ProfilesController : ControllerBase
{
    private readonly IProfileService _profiles;

    /// <summary>Initializes a new instance of the <see cref="ProfilesController"/> class.</summary>
    /// <param name="profiles">Profil servisi.</param>
    public ProfilesController(IProfileService profiles)
    {
        _profiles = profiles;
    }

    /// <summary>Profiller (tr-TR alfabetik), seçili profil, sınır ve şablonlar.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 · 401.</returns>
    [HttpGet]
    public Task<IActionResult> List(CancellationToken cancellationToken) =>
        _profiles.ListAsync(cancellationToken).ToActionResult();

    /// <summary>Profil oluşturur ve seçili yapar.</summary>
    /// <param name="request">Alanlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 profil · 400 alan hatası · 409 sınır · 401.</returns>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] SaveProfileRequest request, CancellationToken cancellationToken) =>
        _profiles.CreateAsync(request, cancellationToken).ToActionResult();

    /// <summary>Seçili profili değiştirir.</summary>
    /// <param name="request">Profil kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 profil · 404 · 401.</returns>
    [HttpPut("selected")]
    public Task<IActionResult> Select([FromBody] SelectProfileRequest request, CancellationToken cancellationToken) =>
        _profiles.SelectAsync(request, cancellationToken).ToActionResult();

    /// <summary>Profili düzenler.</summary>
    /// <param name="id">Profil.</param>
    /// <param name="request">Alanlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 profil · 400 · 404 · 401.</returns>
    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveProfileRequest request, CancellationToken cancellationToken) =>
        _profiles.UpdateAsync(id, request, cancellationToken).ToActionResult();

    /// <summary>Profili kalıcı siler.</summary>
    /// <param name="id">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 yeni liste · 404 · 401.</returns>
    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        _profiles.DeleteAsync(id, cancellationToken).ToActionResult();
}
