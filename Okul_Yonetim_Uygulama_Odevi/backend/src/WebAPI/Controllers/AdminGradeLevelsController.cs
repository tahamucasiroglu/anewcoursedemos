using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration.GradeLevels;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Sınıf seviyesi yönetimi (spec 0008): yalnızca seçili okulun kurucusu/yöneticisi (değilse 403). Başka okulun seviyesi
/// 404; doğrulama hatası ve iş kuralı (aynı ad) 400 ve Türkçe mesaj.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantFounderPolicy)]
[Route("api/admin/grade-levels")]
public sealed class AdminGradeLevelsController : ControllerBase
{
    private readonly ISender sender;

    public AdminGradeLevelsController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GradeLevelAdminView>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GradeLevelAdminView>>> List(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListAdminGradeLevelsQuery(), cancellationToken).ConfigureAwait(false));

    [HttpPost]
    [ProducesResponseType<GradeLevelAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GradeLevelAdminView>> Create(GradeLevelRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new CreateGradeLevelCommand(request.Name, request.SortOrder), cancellationToken).ConfigureAwait(false));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<GradeLevelAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GradeLevelAdminView>> Update(Guid id, GradeLevelRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new UpdateGradeLevelCommand(id, request.Name, request.SortOrder), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType<GradeLevelAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GradeLevelAdminView>> Deactivate(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetGradeLevelActiveCommand(id, false), cancellationToken).ConfigureAwait(false));

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType<GradeLevelAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GradeLevelAdminView>> Activate(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetGradeLevelActiveCommand(id, true), cancellationToken).ConfigureAwait(false));
}
