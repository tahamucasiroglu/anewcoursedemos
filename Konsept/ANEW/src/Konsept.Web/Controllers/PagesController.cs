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

    /// <summary>Beklenmeyen hata sayfası (ayrıntı göstermez).</summary>
    /// <returns>Sayfa.</returns>
    [HttpGet("/hata")]
    public IActionResult Hata() => View();
}
