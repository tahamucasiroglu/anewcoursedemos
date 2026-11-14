using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Api.Controllers;

/// <summary>
/// Kanal profilleri uçları (oturumdaki kullanıcının kayıtları): liste, hazır şablonlar, tek profil, ekle, güncelle,
/// aktif yap (kullanıcı başına tek aktif) ve sil. Doğrulama global filtrede (<c>SaveProfileRequestValidator</c>).
/// </summary>
/// <param name="service">Profil servisi.</param>
[Route("api/profiles")]
public sealed class ProfilesController(IProfileService service) : ApiControllerBase
{
    /// <summary>Kullanıcının profilleri.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ProfileDto"/> listesi.</returns>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => ToActionResult(await service.ListAsync(ct));

    /// <summary>Hazır şablonlar.</summary>
    /// <returns><see cref="ProfileTemplateDto"/> listesi.</returns>
    [HttpGet("templates")]
    public IActionResult Templates() => ToActionResult(service.GetTemplates());

    /// <summary>Tek profil.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ProfileDto"/>.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => ToActionResult(await service.GetAsync(id, ct));

    /// <summary>Yeni profil (aktif olur).</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>201 + <see cref="ProfileDto"/>.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveProfileRequest request, CancellationToken ct) =>
        ToCreatedResult(await service.CreateAsync(request, ct));

    /// <summary>Profili günceller.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ProfileDto"/>.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveProfileRequest request, CancellationToken ct) =>
        ToActionResult(await service.UpdateAsync(id, request, ct));

    /// <summary>Profili aktif yapar.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ProfileDto"/>.</returns>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct) => ToActionResult(await service.ActivateAsync(id, ct));

    /// <summary>Profili siler.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => ToActionResult(await service.DeleteAsync(id, ct));
}
