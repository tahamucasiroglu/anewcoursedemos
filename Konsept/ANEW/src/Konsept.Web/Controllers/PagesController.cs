using Konsept.Web.Auth;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// Üst bardaki sayfalar. Bu spec'te hepsi boş durumlarıyla gelir; içerikleri kendi spec'lerinde (0005…0007, 0016).
/// </summary>
public sealed class PagesController : Controller
{
    /// <summary>Konseptler (ana sayfa).</summary>
    /// <returns>Sayfa.</returns>
    [HttpGet("/")]
    public IActionResult Konseptler() => View();

    /// <summary>Konseptten Video.</summary>
    /// <returns>Sayfa.</returns>
    [HttpGet("/konseptten-video")]
    public IActionResult KonsepttenVideo() => View();

    /// <summary>Üretilenler.</summary>
    /// <returns>Sayfa.</returns>
    [HttpGet("/uretilenler")]
    public IActionResult Uretilenler() => View();

    /// <summary>Silinenler.</summary>
    /// <returns>Sayfa.</returns>
    [HttpGet("/silinenler")]
    public IActionResult Silinenler() => View();

    /// <summary>Yayınlama ("Çok yakında sizlerleyiz").</summary>
    /// <returns>Sayfa.</returns>
    [HttpGet("/yayinlama")]
    public IActionResult Yayinlama() => View();

    /// <summary>
    /// Yetkisiz sayfa (spec 0011 Req 3): "Bu sayfaya erişim yetkiniz yok", 403 durum koduyla. Oturumlu her kullanıcıya açıktır;
    /// yetki politikası reddedince çerez şeması buraya yönlendirir.
    /// </summary>
    /// <returns>Sayfa (403).</returns>
    [HttpGet(WebPolicies.ForbiddenPath)]
    public IActionResult Yetkisiz() => View().WithStatusCode(StatusCodes.Status403Forbidden);

    /// <summary>
    /// Beklenmeyen hata sayfası (ayrıntı göstermez). Hata işleyicisi isteği özgün fiiliyle yeniden çalıştırdığından her fiille
    /// ve kimliksiz açılır (AC-19, review R3-3, plan A-9): ör. kimliksiz POST /giris çökerse de genel hata görünür. Durum
    /// değiştirmez; CSRF doğrulaması gerekmez.
    /// </summary>
    /// <returns>Sayfa.</returns>
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [Route("/hata")]
    public IActionResult Hata() => View();
}
