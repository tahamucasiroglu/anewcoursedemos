using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Services.Account.Abstract;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// İlk çalıştırma: veritabanında kullanıcı yokken tek yönetici hesabını oluşturur (API <c>api/identity/setup</c>).
/// Kurulum tamamlandıysa sayfa giriş ekranına yönlendirir.
/// </summary>
[AllowAnonymous]
public sealed class KurulumController : WebControllerBase
{
    private readonly IAccountService _account;

    /// <summary>Oturum servisini alır.</summary>
    /// <param name="account">Oturum servisi.</param>
    public KurulumController(IAccountService account) => _account = account;

    /// <summary>Kurulum sayfası.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) =>
        await _account.IsSetupRequiredAsync(ct) ? View() : RedirectToAction("Login", "Account");

    /// <summary>Ajax: yönetici hesabını oluşturur; başarıda <see cref="RedirectResponse"/> döner.</summary>
    /// <param name="request">Kurulum bilgileri.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public async Task<IActionResult> Index([FromBody] SetupRequest request, CancellationToken ct) =>
        ToActionResult(await _account.SetupAsync(request, ct));
}
