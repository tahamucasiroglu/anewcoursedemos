using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Identity.Api.RateLimiting;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Api.Controllers;

/// <summary>
/// Kimlik doğrulama uçları (<c>api/identity</c>): <c>GET registration/status</c> (anonim), <c>POST register</c> (anonim, IP başına sınırlı;
/// normal kullanıcı açar, <see cref="TokenResponse"/>; kayıt kapalıysa 403), <c>POST login</c> (anonim; kullanıcı adı ya da e-posta),
/// <c>POST refresh</c> (anonim, rotasyon), <c>POST logout-all</c> (oturum gerekli; ajan dahil tüm token'lar iptal, 204).
/// </summary>
[Route("api/identity")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;

    /// <summary>Servisi alır.</summary>
    /// <param name="auth">Kimlik doğrulama servisi.</param>
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Kayıt durumu.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="RegistrationStatusResponse"/>.</returns>
    [AllowAnonymous]
    [HttpGet("registration/status")]
    public async Task<IActionResult> RegistrationStatus(CancellationToken ct) => ToActionResult(await _auth.GetRegistrationStatusAsync(ct));

    /// <summary>Yeni normal kullanıcı hesabı açar ve oturum açar.</summary>
    /// <param name="request">Kullanıcı adı + parola.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="TokenResponse"/>.</returns>
    [AllowAnonymous]
    [EnableRateLimiting(IdentityRateLimitPolicies.RegisterPolicy)]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct) => ToActionResult(await _auth.RegisterAsync(request, ct));

    /// <summary>Giriş (kullanıcı adı ya da e-posta).</summary>
    /// <param name="request">Kullanıcı adı / e-posta + parola.</param>
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
