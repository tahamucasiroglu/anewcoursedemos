using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// Günlük geçmişi modalının Ajax ucu (spec 0021 Req 5–7). Giriş ister (default-deny); sahiplik Api'dedir, Web yalnızca iletir.
/// </summary>
public sealed class JournalController : Controller
{
    private readonly IKonseptApiClient _api;

    /// <summary>Initializes a new instance of the <see cref="JournalController"/> class.</summary>
    /// <param name="api">API istemcisi.</param>
    public JournalController(IKonseptApiClient api)
    {
        _api = api;
    }

    /// <summary>Bir günün kayıtları (Ajax).</summary>
    /// <param name="tarih">Gün (<c>yyyy-MM-dd</c>).</param>
    /// <param name="filtre">Seviye filtresi.</param>
    /// <param name="atla">Atlanacak kayıt sayısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/gunluk")]
    public Task<IActionResult> Day(string? tarih, string? filtre, int atla, CancellationToken cancellationToken) =>
        _api.GetJournalDayAsync(tarih, filtre, atla, cancellationToken).ToBrowserJson(HttpContext);
}
