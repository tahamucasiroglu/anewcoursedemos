using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration.ClassroomTeachers;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Sınıf öğretmeni ataması (spec 0010): yalnızca seçili okulun kurucusu/yöneticisi (değilse 403). Başka okulun sınıfı 404;
/// sınıfın şubesinde etkin öğretmen olmayan kişi 400 (hiçbir şey değişmez).
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantFounderPolicy)]
[Route("api/admin/classrooms/{id:guid}/teachers")]
public sealed class AdminClassroomTeachersController : ControllerBase
{
    private readonly ISender sender;

    public AdminClassroomTeachersController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<ClassroomTeachersView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomTeachersView>> List(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListClassroomTeachersQuery(id), cancellationToken).ConfigureAwait(false));

    [HttpPut]
    [ProducesResponseType<ClassroomTeachersView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomTeachersView>> Set(Guid id, ClassroomTeachersRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new SetClassroomTeachersCommand(id, request.TeacherUserIds), cancellationToken).ConfigureAwait(false));
    }
}
