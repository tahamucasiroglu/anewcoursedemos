using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Eski ilk kurulum adresi (<c>/Kurulum</c>) — geriye dönük uyumluluk için yalnız yönlendirir: kayıt sayfasına
/// (<c>/Account/Register</c>). Tek seferlik kurulum kaldırıldı: yönetici yapılandırmadan tohumlanır, herkes Kayıt ol ile hesap açar.
/// </summary>
[AllowAnonymous]
public sealed class KurulumController : WebControllerBase
{
    /// <summary>Kayıt sayfasına yönlendirir.</summary>
    [HttpGet]
    public IActionResult Index() => RedirectToAction("Register", "Account");
}
