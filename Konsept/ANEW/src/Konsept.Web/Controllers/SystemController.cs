using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// Tarayıcının <c>$.ajax</c> ile çağırdığı sistem uçları; her şeyi API'ye sorar (tarayıcı → MVC → API).
/// </summary>
[Route("system")]
public sealed class SystemController : Controller
{
    private readonly IKonseptApiClient _api;

    /// <summary>Initializes a new instance of the <see cref="SystemController"/> class.</summary>
    /// <param name="api">API istemcisi.</param>
    public SystemController(IKonseptApiClient api)
    {
        _api = api;
    }

    /// <summary>Durum alanı: sürüm, API, veritabanı, ajan, son Günlük olayı (3 sn'de bir yoklanır).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("status")]
    public Task<IActionResult> Status(CancellationToken cancellationToken) =>
        _api.GetSystemStatusAsync(cancellationToken).ToStatusJson(HttpContext);

    /// <summary>"Ajanı test et".</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("agent-test")]
    public Task<IActionResult> AgentTest(CancellationToken cancellationToken) =>
        _api.TestAgentAsync(cancellationToken).ToBrowserJson(HttpContext);
}
