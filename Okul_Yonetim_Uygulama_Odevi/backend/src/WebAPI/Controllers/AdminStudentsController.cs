using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Commands.CreateStudent;
using OkulYonetim.Application.Features.Students.Commands.LinkGuardian;
using OkulYonetim.Application.Features.Students.Commands.SetPickupPersons;
using OkulYonetim.Application.Features.Students.Commands.SetStudentActive;
using OkulYonetim.Application.Features.Students.Commands.SetStudentHealth;
using OkulYonetim.Application.Features.Students.Commands.UnlinkGuardian;
using OkulYonetim.Application.Features.Students.Commands.UpdateStudent;
using OkulYonetim.Application.Features.Students.Queries.GetStudent;
using OkulYonetim.Application.Features.Students.Queries.ListStudents;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Öğrenci yönetimi (spec 0009): liste, kart, ekle, düzenle, sağlık, ayrıldı/geri al, veli bağla/kaldır, teslim
/// alabilecek kişiler. Yalnızca seçili okulun kurucusu/yöneticisi (değilse 403); başka okulun öğrencisi 404. Geçici şifre
/// yalnızca veli bağlama yanıtında, bir kez döner. Değişiklik yanıtları yalnızca kimlik taşır; kart ayrıca okunur.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantFounderPolicy)]
[Route("api/admin/students")]
public sealed class AdminStudentsController : ControllerBase
{
    private readonly ISender sender;

    public AdminStudentsController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<StudentPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentPage>> List(
        [FromQuery] string? search,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? classroomId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default) =>
        this.Ok(await this.sender.Send(new ListStudentsQuery(search, branchId, classroomId, status, page), cancellationToken).ConfigureAwait(false));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<StudentDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentDetail>> Get(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetStudentQuery(id), cancellationToken).ConfigureAwait(false));

    [HttpPost]
    [ProducesResponseType<StudentSaved>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StudentSaved>> Create(StudentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(
            new CreateStudentCommand(request.FullName, request.BirthDate, request.SchoolNumber, request.BranchId, request.ClassroomId, request.HealthNote, request.Allergy),
            cancellationToken).ConfigureAwait(false));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<StudentSaved>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentSaved>> Update(Guid id, StudentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(
            new UpdateStudentCommand(id, request.FullName, request.BirthDate, request.SchoolNumber, request.BranchId, request.ClassroomId),
            cancellationToken).ConfigureAwait(false));
    }

    [HttpPut("{id:guid}/health")]
    [ProducesResponseType<StudentSaved>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentSaved>> SetHealth(Guid id, StudentHealthRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new SetStudentHealthCommand(id, request.HealthNote, request.Allergy), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType<StudentSaved>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentSaved>> Deactivate(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetStudentActiveCommand(id, false), cancellationToken).ConfigureAwait(false));

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType<StudentSaved>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentSaved>> Activate(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetStudentActiveCommand(id, true), cancellationToken).ConfigureAwait(false));

    [HttpPost("{id:guid}/guardians")]
    [ProducesResponseType<LinkGuardianResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LinkGuardianResult>> LinkGuardian(Guid id, LinkGuardianRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(
            new LinkGuardianCommand(id, request.FullName, request.Email, request.Phone, request.Relationship), cancellationToken).ConfigureAwait(false));
    }

    [HttpDelete("{id:guid}/guardians/{userId:guid}")]
    [ProducesResponseType<StudentSaved>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentSaved>> UnlinkGuardian(Guid id, Guid userId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new UnlinkGuardianCommand(id, userId), cancellationToken).ConfigureAwait(false));

    [HttpPut("{id:guid}/pickup-persons")]
    [ProducesResponseType<StudentSaved>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentSaved>> SetPickupPersons(Guid id, PickupPersonsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new SetPickupPersonsCommand(id, request.Persons ?? []), cancellationToken).ConfigureAwait(false));
    }
}
