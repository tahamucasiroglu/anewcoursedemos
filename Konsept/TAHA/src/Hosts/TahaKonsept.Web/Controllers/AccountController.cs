using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TahaKonsept.Web.Controllers.Base;
using TahaKonsept.Web.Infrastructure.RateLimiting;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Services.Account.Abstract;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Giriş / kayıt / çıkış. Sayfalar: <see cref="Login(string?)"/>, <see cref="Register(CancellationToken)"/> (kayıt kapalıysa mesaj).
/// Ajax: <see cref="Login(LoginRequest, CancellationToken)"/> (API <c>api/identity/login</c> → cookie; kullanıcı adı ya da e-posta),
/// <see cref="Register(RegisterRequest, CancellationToken)"/> (API <c>api/identity/register</c> → cookie → Başlangıç),
/// <see cref="Logout"/> (API <c>api/identity/logout-all</c> → cookie silinir). Giriş/kayıt IP başına sınırlı (<see cref="AccountRateLimitPolicies"/>).
/// Oturum açıkken giriş/kayıt sayfası ana sayfaya yönlendirir.
/// </summary>
[AllowAnonymous]
public sealed class AccountController : WebControllerBase
{
    private readonly IAccountService _account;

    /// <summary>Oturum servisini alır.</summary>
    /// <param name="account">Oturum servisi.</param>
    public AccountController(IAccountService account) => _account = account;

    /// <summary>Giriş sayfası.</summary>
    /// <param name="returnUrl">Girişten sonra dönülecek adres.</param>
    [HttpGet]
    public IActionResult Login(string? returnUrl) =>
        User.Identity?.IsAuthenticated == true ? Redirect("/") : View(new LoginPageModel { ReturnUrl = returnUrl });

    /// <summary>Ajax giriş; başarıda <see cref="RedirectResponse"/> döner.</summary>
    /// <param name="request">Giriş bilgileri.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    [EnableRateLimiting(AccountRateLimitPolicies.Login)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct) =>
        ToActionResult(await _account.LoginAsync(request, ct));

    /// <summary>Kayıt sayfası.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public async Task<IActionResult> Register(CancellationToken ct) =>
        User.Identity?.IsAuthenticated == true
            ? Redirect("/")
            : View(new RegisterPageModel { RegistrationOpen = await _account.IsRegistrationOpenAsync(ct) });

    /// <summary>Ajax kayıt; başarıda <see cref="RedirectResponse"/> (Başlangıç) döner.</summary>
    /// <param name="request">Kayıt bilgileri.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    [EnableRateLimiting(AccountRateLimitPolicies.Register)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct) =>
        ToActionResult(await _account.RegisterAsync(request, ct));

    /// <summary>Ajax çıkış (tüm cihazlar, ajan dahil).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public async Task<IActionResult> Logout(CancellationToken ct) =>
        ToActionResult(await _account.LogoutAsync(ct));
}
