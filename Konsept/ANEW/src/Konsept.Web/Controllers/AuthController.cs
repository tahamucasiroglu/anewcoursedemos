using Konsept.Web.Auth;
using Konsept.Web.Auth.Abstract;
using Konsept.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// Giriş (<c>/giris</c>), kayıt (<c>/kayit</c>) ve çıkış (<c>/cikis</c>) sayfaları (spec 0002). Giriş ve kayıt anonimdir
/// (Req 11 istisnası); çıkış giriş ister ve CSRF belirteci ister.
/// </summary>
public sealed class AuthController : Controller
{
    private readonly IAuthPageService _pages;

    /// <summary>Initializes a new instance of the <see cref="AuthController"/> class.</summary>
    /// <param name="pages">Sayfa servisi.</param>
    public AuthController(IAuthPageService pages)
    {
        _pages = pages;
    }

    /// <summary>Giriş sayfası.</summary>
    /// <param name="returnUrl">Girişten sonra dönülecek site içi adres.</param>
    /// <param name="oturum">"bitti" ise "Oturumunuz sona erdi" bilgisi gösterilir.</param>
    /// <returns>Sayfa ya da yönlendirme.</returns>
    [AllowAnonymous]
    [HttpGet("/giris")]
    public IActionResult Giris(string? returnUrl, string? oturum) => _pages.ShowLogin(this, returnUrl, oturum == "bitti");

    /// <summary>Giriş formu.</summary>
    /// <param name="form">Form.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yönlendirme ya da hatalı form.</returns>
    [AllowAnonymous]
    [RedirectSignedIn]
    [HttpPost("/giris")]
    public Task<IActionResult> Giris([FromForm] LoginForm form, CancellationToken cancellationToken) => _pages.LoginAsync(this, form, cancellationToken);

    /// <summary>Kayıt sayfası.</summary>
    /// <param name="returnUrl">Kayıttan sonra dönülecek site içi adres.</param>
    /// <returns>Sayfa ya da yönlendirme.</returns>
    [AllowAnonymous]
    [HttpGet("/kayit")]
    public IActionResult Kayit(string? returnUrl) => _pages.ShowRegister(this, returnUrl);

    /// <summary>Kayıt formu.</summary>
    /// <param name="form">Form.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yönlendirme ya da hatalı form.</returns>
    [AllowAnonymous]
    [RedirectSignedIn]
    [HttpPost("/kayit")]
    public Task<IActionResult> Kayit([FromForm] RegisterForm form, CancellationToken cancellationToken) => _pages.RegisterAsync(this, form, cancellationToken);

    /// <summary>Çıkış: tüm oturumları kapatır.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Giriş sayfasına yönlendirme.</returns>
    [HttpPost("/cikis")]
    public Task<IActionResult> Cikis(CancellationToken cancellationToken) => _pages.LogoutAsync(this, cancellationToken);
}
