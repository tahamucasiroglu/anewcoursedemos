using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Api.Controllers;

/// <summary>
/// Yönetici kayıt ayarı (<c>api/identity/admin/registration</c>, yalnız <see cref="PermissionNames.AdminAccess"/>):
/// <c>GET</c> → <see cref="RegistrationSettingsDto"/>, <c>PUT</c> → kaydeder ve döner. Servis de ayrıca yetki denetler.
/// </summary>
[Route("api/identity/admin/registration")]
[Authorize(Policy = PermissionNames.AdminAccess)]
public sealed class RegistrationSettingsController : ApiControllerBase
{
    private readonly IRegistrationSettingsService _settings;

    /// <summary>Servisi alır.</summary>
    /// <param name="settings">Kayıt ayarı servisi.</param>
    public RegistrationSettingsController(IRegistrationSettingsService settings) => _settings = settings;

    /// <summary>Ayarı okur.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="RegistrationSettingsDto"/>.</returns>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => ToActionResult(await _settings.GetAsync(ct));

    /// <summary>Ayarı kaydeder.</summary>
    /// <param name="settings">Yeni ayar.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kaydedilen <see cref="RegistrationSettingsDto"/>.</returns>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] RegistrationSettingsDto settings, CancellationToken ct) => ToActionResult(await _settings.UpdateAsync(settings, ct));
}
