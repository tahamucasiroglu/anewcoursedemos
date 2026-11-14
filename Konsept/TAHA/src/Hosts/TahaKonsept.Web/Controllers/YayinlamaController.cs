using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;

namespace TahaKonsept.Web.Controllers;

/// <summary>Yayınlama sayfası — şimdilik yalnızca "Çok yakında sizlerleyiz" kartı.</summary>
public sealed class YayinlamaController : WebControllerBase
{
    /// <summary>Sayfa.</summary>
    [HttpGet]
    public IActionResult Index() => View();
}
