using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Api.Controllers;

/// <summary>
/// Kimlik doğrulama uçları (<c>api/identity</c>): <c>GET setup/status</c> (anonim), <c>POST setup</c> (anonim, yalnızca hiç
/// kullanıcı yokken; <see cref="TokenResponse"/>), <c>POST login</c> (anonim), <c>POST refresh</c> (anonim, rotasyon),
/// <c>POST logout-all</c> (oturum gerekli; ajan dahil tüm token'lar iptal, 204).
/// </summary>
[Route("api/identity")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;

    /// <summary>Servisi alır.</summary>
    /// <param name="auth">Kimlik doğrulama servisi.</param>
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Kurulum gerekli mi.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="SetupStatusResponse"/>.</returns>
    [AllowAnonymous]
    [HttpGet("setup/status")]
    public async Task<IActionResult> SetupStatus(CancellationToken ct) => ToActionResult(await _auth.GetSetupStatusAsync(ct));

    /// <summary>Tek yönetici hesabını oluşturur ve oturum açar.</summary>
    /// <param name="request">Kullanıcı adı + parola.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="TokenResponse"/>.</returns>
    [AllowAnonymous]
    [HttpPost("setup")]
    public async Task<IActionResult> Setup([FromBody] SetupRequest request, CancellationToken ct) => ToActionResult(await _auth.SetupAsync(request, ct));

    /// <summary>Giriş.</summary>
    /// <param name="request">Kullanıcı adı + parola.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="TokenResponse"/>.</returns>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct) => ToActionResult(await _auth.LoginAsync(request, ct));

    /// <summary>Token yenileme (rotasyon).</summary>
    /// <param name="request">Refresh token.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="TokenResponse"/>.</returns>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct) => ToActionResult(await _auth.RefreshAsync(request, ct));

    /// <summary>Tüm oturumlardan çıkış (ajan dahil).</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken ct) => ToActionResult(await _auth.LogoutAllAsync(ct));
}
