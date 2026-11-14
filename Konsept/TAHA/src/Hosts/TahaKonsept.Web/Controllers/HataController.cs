using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;
using TahaKonsept.Web.Models;

namespace TahaKonsept.Web.Controllers;

/// <summary>Beklenmeyen hata ve durum kodu (404 vb.) sayfası.</summary>
[AllowAnonymous]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class HataController : WebControllerBase
{
    /// <summary>Hata sayfası.</summary>
    /// <param name="code">HTTP durum kodu (UseStatusCodePagesWithReExecute'tan).</param>
    [HttpGet]
    public IActionResult Index(int? code) =>
        View(new ErrorPageModel { StatusCode = code ?? 500, TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
