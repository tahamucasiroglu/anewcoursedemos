using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Communication;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Velinin bildirimleri (spec 0010; okul seçilmiş olmalı): liste (yalnızca kendi bağlı çocuklarınınki, yeniden eskiye, en
/// fazla 50) ve "gösterilenler okundu" (yalnızca listede gösterilen bildirimler; review F-3). Veli rolü olmayan 403. Spec 0014: tek
/// bildirimin ayrıntısı (haftalık gelişim özeti; yalnızca kendi bağlı çocuğununki — değilse 404; açılış okundu yapar).
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType<NoticeView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoticeView>> Get(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetMyNoticeQuery(id), cancellationToken).ConfigureAwait(false));

    [HttpPost("read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkRead(MarkNoticesReadRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await this.sender.Send(new MarkMyNoticesReadCommand(request.UpTo), cancellationToken).ConfigureAwait(false);
        return this.NoContent();
    }
}
