using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.Auth;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// Admin alanı (spec 0011 Req 4): yalnızca <c>admin.panel</c> yetkisi olanlara açık iskelet sayfa ve özet Ajax ucu.
/// Yetkisiz sayfa isteği "/yetkisiz"e, yetkisiz Ajax 403 JSON'a gider; asıl karar Api'dedir (özet Api'den istenir).
/// </summary>
[Authorize(Policy = WebPolicies.AdminPanel)]
public sealed class AdminController : Controller
{
    private readonly IKonseptApiClient _api;

    /// <summary>Initializes a new instance of the <see cref="AdminController"/> class.</summary>
    /// <param name="api">API istemcisi.</param>
    public AdminController(IKonseptApiClient api)
    {
        _api = api;
    }

    /// <summary>Admin iskelet sayfası (boş durum metni).</summary>
    /// <returns>Sayfa.</returns>
    [HttpGet("/admin")]
    public IActionResult Index() => View();

    /// <summary>Admin özeti (Ajax; Api'den).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/admin/ozet")]
    public Task<IActionResult> Ozet(CancellationToken cancellationToken) =>
        _api.GetAdminOverviewAsync(cancellationToken).ToBrowserJson(HttpContext);
}
