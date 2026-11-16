using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.Infrastructure;
using Konsept.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// "Ajanı eşleştir" (spec 0012 Req 3–8): giriş isteyen ayrı sayfa (default-deny) ve Ajax uçları (CSRF korumalı POST). Kod
/// yalnızca istek gövdesinde gider, adreste değil; tarayıcının IP'si Api'nin hız sınırı için iletilir. Kararlar Api'dedir.
/// </summary>
public sealed class AgentPairingController : Controller
{
    private readonly IKonseptApiClient _api;

    /// <summary>Initializes a new instance of the <see cref="AgentPairingController"/> class.</summary>
    /// <param name="api">API istemcisi.</param>
    public AgentPairingController(IKonseptApiClient api)
    {
        _api = api;
    }

    /// <summary>Eşleştirme sayfası.</summary>
    /// <returns>Sayfa.</returns>
    [HttpGet("/ajan-eslestir")]
    public IActionResult Index() => View();

    /// <summary>Kodun cihazını sorar (Ajax).</summary>
    /// <param name="form">Kod.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON: makine adı + sürüm ya da Türkçe hata.</returns>
    [HttpPost("/ajan-eslestir/sorgula")]
    public Task<IActionResult> Lookup([FromBody] PairingCodeForm? form, CancellationToken cancellationToken) =>
        _api.AgentPairingAsync("lookup", form?.Code, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken).ToBrowserJson(HttpContext);

    /// <summary>Kodu onaylar (Ajax).</summary>
    /// <param name="form">Kod.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/ajan-eslestir/onayla")]
    public Task<IActionResult> Approve([FromBody] PairingCodeForm? form, CancellationToken cancellationToken) =>
        _api.AgentPairingAsync("approve", form?.Code, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken).ToBrowserJson(HttpContext);

    /// <summary>Kodu reddeder (Ajax).</summary>
    /// <param name="form">Kod.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/ajan-eslestir/reddet")]
    public Task<IActionResult> Reject([FromBody] PairingCodeForm? form, CancellationToken cancellationToken) =>
        _api.AgentPairingAsync("reject", form?.Code, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken).ToBrowserJson(HttpContext);
}
