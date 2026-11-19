using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Identity.Queries.GetMe;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>"Ben kimim" ucu (giriş gerekli; okul seçilmemişse okul ve şubeler boş, okul listesi dolu).</summary>
[ApiController]
[Produces("application/json")]
[Route("api/me")]
public sealed class MeController : ControllerBase
{
    private readonly ISender sender;

    public MeController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetMeQuery(), cancellationToken).ConfigureAwait(false));
}
