using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration.Classrooms;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Sınıf (şube-sınıf eşleşmesi) yönetimi (spec 0008): yalnızca seçili okulun kurucusu/yöneticisi (değilse 403). Liste
/// dönem (varsayılan güncel), şube ve durum (<c>active</c>) ile süzülür. Başka okulun sınıfı/dönemi 404; başka okulun ya
/// da pasif şube/seviyesi 400.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantFounderPolicy)]
[Route("api/admin/classrooms")]
public sealed class AdminClassroomsController : ControllerBase
{
    private readonly ISender sender;

    public AdminClassroomsController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<ClassroomAdminList>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomAdminList>> List([FromQuery] Guid? termId, [FromQuery] Guid? branchId, [FromQuery] bool? active, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListAdminClassroomsQuery(termId, branchId, active), cancellationToken).ConfigureAwait(false));

    [HttpPost]
    [ProducesResponseType<ClassroomAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomAdminView>> Create(ClassroomRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new CreateClassroomCommand(request.TermId, request.BranchId, request.GradeLevelId, request.Name), cancellationToken).ConfigureAwait(false));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ClassroomAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomAdminView>> Update(Guid id, ClassroomRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new UpdateClassroomCommand(id, request.GradeLevelId, request.Name), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType<ClassroomAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomAdminView>> Deactivate(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetClassroomActiveCommand(id, false), cancellationToken).ConfigureAwait(false));

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType<ClassroomAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomAdminView>> Activate(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetClassroomActiveCommand(id, true), cancellationToken).ConfigureAwait(false));
}
