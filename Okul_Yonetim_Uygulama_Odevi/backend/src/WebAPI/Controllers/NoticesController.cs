using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Communication;
using OkulYonetim.WebAPI.Auth;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Velinin bildirimleri (spec 0010; okul seçilmiş olmalı): liste (yalnızca kendi bağlı çocuklarınınki, yeniden eskiye, en
/// fazla 50) ve "hepsi okundu". Veli rolü olmayan 403.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/notices")]
public sealed class NoticesController : ControllerBase
{
    private readonly ISender sender;

    public NoticesController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<NoticeView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<NoticeView>>> List(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListMyNoticesQuery(), cancellationToken).ConfigureAwait(false));

    [HttpPost("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkRead(CancellationToken cancellationToken)
    {
        await this.sender.Send(new MarkMyNoticesReadCommand(), cancellationToken).ConfigureAwait(false);
        return this.NoContent();
    }
}
