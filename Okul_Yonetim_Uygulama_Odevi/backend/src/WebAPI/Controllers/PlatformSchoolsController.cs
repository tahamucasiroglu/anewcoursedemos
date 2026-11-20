using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Platform;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Platform paneli (spec 0007): okul listesi, okul ekle, pasifleştir / etkinleştir. Yalnızca platform yöneticisi ve
/// iki adımlı doğrulamayla açılmış oturum (değilse 403). Okulların içindeki veri dönmez.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.PlatformAdminPolicy)]
[Route("api/platform/schools")]
public sealed class PlatformSchoolsController : ControllerBase
{
    private readonly ISender sender;

    public PlatformSchoolsController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<SchoolPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SchoolPage>> List([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        this.Ok(await this.sender.Send(new ListSchoolsQuery(search, status, page), cancellationToken).ConfigureAwait(false));

    [HttpPost]
    [ProducesResponseType<PanelSchoolResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PanelSchoolResult>> Create(CreateSchoolRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new CreateSchoolFromPanelCommand(request.SchoolName ?? string.Empty, request.BranchName ?? string.Empty, request.FounderName ?? string.Empty, request.FounderEmail ?? string.Empty), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType<SchoolRow>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SchoolRow>> Deactivate(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetSchoolActiveCommand(id, false), cancellationToken).ConfigureAwait(false));

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType<SchoolRow>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SchoolRow>> Activate(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetSchoolActiveCommand(id, true), cancellationToken).ConfigureAwait(false));
}
