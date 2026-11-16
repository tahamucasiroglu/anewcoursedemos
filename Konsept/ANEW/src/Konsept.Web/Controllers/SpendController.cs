using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>Üst bardaki aylık harcama (spec 0013 Req 15–18; Ajax). Giriş ister (default-deny); toplam Api'de kullanıcıya göre süzülür.</summary>
public sealed class SpendController : Controller
{
    private readonly IKonseptApiClient _api;

    /// <summary>Initializes a new instance of the <see cref="SpendController"/> class.</summary>
    /// <param name="api">API istemcisi.</param>
    public SpendController(IKonseptApiClient api)
    {
        _api = api;
    }

    /// <summary>Bu ayın harcaması, gösterime hazır (<c>$2,37</c>).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/harcama")]
    public Task<IActionResult> Month(CancellationToken cancellationToken) =>
        _api.GetMonthlySpendAsync(cancellationToken).ToSpendJson(HttpContext);
}
