using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Services.Account.Abstract;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Giriş / çıkış. <see cref="Login(string?, CancellationToken)"/> sayfası (kurulum gerekiyorsa /Kurulum'a yönlendirir),
/// <see cref="Login(LoginRequest, CancellationToken)"/> Ajax girişi (API <c>api/identity/login</c> → cookie),
/// <see cref="Logout"/> Ajax çıkışı (API <c>api/identity/logout-all</c> → cookie silinir).
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
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl, CancellationToken ct) =>
        await _account.IsSetupRequiredAsync(ct) ? RedirectToAction("Index", "Kurulum") : View(new LoginPageModel { ReturnUrl = returnUrl });

    /// <summary>Ajax giriş; başarıda <see cref="RedirectResponse"/> döner.</summary>
    /// <param name="request">Giriş bilgileri.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct) =>
        ToActionResult(await _account.LoginAsync(request, ct));

    /// <summary>Ajax çıkış (tüm cihazlar, ajan dahil).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public async Task<IActionResult> Logout(CancellationToken ct) =>
        ToActionResult(await _account.LogoutAsync(ct));
}
