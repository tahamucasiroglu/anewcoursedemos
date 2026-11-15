using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.Identity.Presentation.Controllers;

/// <summary>
/// Admin alanı uçları (spec 0011 Req 3–4). Yalnızca <c>admin.panel</c> yetkisiyle açılır; yetki her istekte veritabanındaki
/// rolden kurulur. Yetkisiz kullanıcı 403 alır ve hiçbir admin verisi dönmez; kimliksiz istek 401. Admin paneli modül
/// değildir; iskelet uç yetkinin sahibi Identity'dedir (plan Risk 4, 0010 yeniden değerlendirir).
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = AuthPolicies.AdminPanel)]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminOverviewService _overview;

    /// <summary>Initializes a new instance of the <see cref="AdminController"/> class.</summary>
    /// <param name="overview">Admin özeti servisi.</param>
    public AdminController(IAdminOverviewService overview)
    {
        _overview = overview;
    }

    /// <summary>Admin alanının özeti (iskelet: boş bölüm listesi).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>200 özet · 401 kimliksiz · 403 yetkisiz.</returns>
    [HttpGet("overview")]
    public Task<IActionResult> Overview(CancellationToken cancellationToken) =>
        _overview.GetOverviewAsync(cancellationToken).ToActionResult();
}
