using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Konsept.Modules.Identity.Presentation.Controllers;

/// <summary>
/// Kimlik uçları (spec 0002). Kayıt ve giriş anonimdir ve IP başına hız sınırına tabidir; refresh anonimdir ama yalnızca
/// geçerli refresh token'la çalışır (plan Risk 2, A-0); çıkış ve oturum bilgisi giriş ister.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IRegistrationService _registration;
    private readonly ILoginService _login;
    private readonly ISessionService _sessions;

    /// <summary>Initializes a new instance of the <see cref="AuthController"/> class.</summary>
    /// <param name="registration">Kayıt servisi.</param>
    /// <param name="login">Giriş servisi.</param>
    /// <param name="sessions">Oturum servisi.</param>
    public AuthController(IRegistrationService registration, ILoginService login, ISessionService sessions)
    {
        _registration = registration;
        _login = login;
        _sessions = sessions;
    }

    /// <summary>Hesap açar ve oturum başlatır.</summary>
    /// <param name="request">Kayıt isteği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 token çifti · 400 alan hataları · 409 e-posta kayıtlı · 429 çok fazla deneme.</returns>
    [AllowAnonymous]
    [EnableRateLimiting(AuthPolicies.AuthRateLimit)]
    [HttpPost("register")]
    public Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken) =>
        _registration.RegisterAsync(request, cancellationToken).ToActionResult();

    /// <summary>E-posta ve şifreyle giriş yapar.</summary>
    /// <param name="request">Giriş isteği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 token çifti · 400 alan hataları · 401 kimlik hatası (kalan hak / kilit) · 429 çok fazla deneme.</returns>
    [AllowAnonymous]
    [EnableRateLimiting(AuthPolicies.AuthRateLimit)]
    [HttpPost("login")]
    public Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        _login.LoginAsync(request, cancellationToken).ToActionResult();

    /// <summary>Refresh token'ı döndürür, yeni erişim token'ı verir.</summary>
    /// <param name="request">Refresh isteği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 token çifti · 401 oturum geçersiz.</returns>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken) =>
        _sessions.RefreshAsync(request, cancellationToken).ToActionResult();

    /// <summary>Kullanıcının tüm oturumlarını kapatır (BR-14).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 iptal edilen oturum sayısı.</returns>
    [HttpPost("logout")]
    public Task<IActionResult> Logout(CancellationToken cancellationToken) =>
        _sessions.LogoutAsync(cancellationToken).ToActionResult();

    /// <summary>Geçerli oturumun kullanıcısı.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 kullanıcı · 401 oturum geçersiz.</returns>
    [HttpGet("session")]
    public Task<IActionResult> Session(CancellationToken cancellationToken) =>
        _sessions.GetCurrentAsync(cancellationToken).ToActionResult();
}
