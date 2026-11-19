using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.SystemHealth.Queries.GetSystemHealth;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Anonim sistem durumu ucu (docs/security.md "varsayılan reddet" kuralının açıkça listelenen istisnası):
/// çalışıyorsa 200, veritabanına ulaşılamıyorsa 503; yanıt yalnızca durum ve Türkçe mesaj içerir.
/// </summary>
[ApiController]
[Produces("application/json")]
[AllowAnonymous]
[Route("api/system-health")]
public sealed class SystemHealthController : ControllerBase
{
    private readonly ISender sender;

    public SystemHealthController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<SystemHealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<SystemHealthResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SystemHealthResponse>> Get(CancellationToken cancellationToken)
    {
        SystemHealthResponse response = await this.sender.Send(new GetSystemHealthQuery(), cancellationToken).ConfigureAwait(false);
        int statusCode = response.Status == "healthy" ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
        return this.StatusCode(statusCode, response);
    }
}
