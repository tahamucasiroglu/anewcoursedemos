using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Communication.WorkingHours;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Öğretmenin kendi mesai saatleri (spec 0011; okul seçilmiş olmalı): oku ve kaydet/kapat. Öğretmen rolü olmayan 403; alan
/// hatası 400.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/me/quiet-hours")]
public sealed class QuietHoursController : ControllerBase
{
    private readonly ISender sender;

    public QuietHoursController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<QuietHoursView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<QuietHoursView>> Get(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetMyQuietHoursQuery(), cancellationToken).ConfigureAwait(false));

    [HttpPut]
    [ProducesResponseType<QuietHoursView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<QuietHoursView>> Set(QuietHoursRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new SetMyQuietHoursCommand(request.Enabled, request.Start, request.End, request.Days), cancellationToken).ConfigureAwait(false));
    }
}
