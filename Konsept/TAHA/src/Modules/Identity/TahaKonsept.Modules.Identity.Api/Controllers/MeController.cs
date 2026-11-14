using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Api.Controllers;

/// <summary>
/// Oturumdaki kullanıcı (<c>api/identity/me</c>): <c>GET</c> → <see cref="CurrentUserResponse"/>,
/// <c>GET|PUT preferences</c> → <see cref="UserPreferencesDto"/> (tema).
/// </summary>
[Route("api/identity/me")]
public sealed class MeController : ApiControllerBase
{
    private readonly IUserProfileService _profile;

    /// <summary>Servisi alır.</summary>
    /// <param name="profile">Kullanıcı profil servisi.</param>
    public MeController(IUserProfileService profile) => _profile = profile;

    /// <summary>Kullanıcı bilgisi.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="CurrentUserResponse"/>.</returns>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => ToActionResult(await _profile.GetMeAsync(ct));

    /// <summary>Tercihler.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="UserPreferencesDto"/>.</returns>
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken ct) => ToActionResult(await _profile.GetPreferencesAsync(ct));

    /// <summary>Tercihleri kaydeder.</summary>
    /// <param name="preferences">Yeni tercihler.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kaydedilen <see cref="UserPreferencesDto"/>.</returns>
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UserPreferencesDto preferences, CancellationToken ct) =>
        ToActionResult(await _profile.UpdatePreferencesAsync(preferences, ct));
}
