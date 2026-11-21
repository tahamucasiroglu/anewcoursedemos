using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Academics.Assessments;
using OkulYonetim.Application.Features.Academics.Assignments;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Öğretmenin ödev ve not uçları (spec 0013; okul seçilmiş olmalı): sınıfın ödevleri ve değerlendirmeleri, teslim ve puan
/// listeleri. Öğretmen rolü olmayan 403; atanmadığı, başka şubenin/okulun ya da pasif sınıf ve o sınıfların kayıtları 404;
/// teslim durumunu ve puanı oluşturan öğretmen dışındaki biri değiştirmek isterse 403. Alan hataları 400.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/academics")]
public sealed class AcademicsController : ControllerBase
{
    private readonly ISender sender;

    public AcademicsController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet("classrooms/{classroomId:guid}/assignments")]
    [ProducesResponseType<IReadOnlyList<AssignmentRow>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AssignmentRow>>> Assignments(Guid classroomId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListClassroomAssignmentsQuery(classroomId), cancellationToken).ConfigureAwait(false));

    [HttpPost("classrooms/{classroomId:guid}/assignments")]
    [ProducesResponseType<AssignmentCreated>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentCreated>> CreateAssignment(Guid classroomId, AssignmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new CreateAssignmentCommand(classroomId, request.Subject, request.Title, request.Description, request.DueDate), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("assignments/{id:guid}/submissions")]
    [ProducesResponseType<AssignmentSubmissionsView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentSubmissionsView>> Submissions(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetAssignmentSubmissionsQuery(id), cancellationToken).ConfigureAwait(false));

    [HttpPut("assignments/{id:guid}/submissions")]
    [ProducesResponseType<SubmissionsSaveResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionsSaveResult>> SaveSubmissions(Guid id, SubmissionsRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new SaveSubmissionsCommand(id, request.Entries), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("classrooms/{classroomId:guid}/assessments")]
    [ProducesResponseType<IReadOnlyList<AssessmentRow>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AssessmentRow>>> Assessments(Guid classroomId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListClassroomAssessmentsQuery(classroomId), cancellationToken).ConfigureAwait(false));

    [HttpPost("classrooms/{classroomId:guid}/assessments")]
    [ProducesResponseType<AssessmentCreated>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssessmentCreated>> CreateAssessment(Guid classroomId, AssessmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new CreateAssessmentCommand(classroomId, request.Subject, request.Kind, request.Title, request.Date), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("assessments/{id:guid}/grades")]
    [ProducesResponseType<AssessmentGradesView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssessmentGradesView>> Grades(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetAssessmentGradesQuery(id), cancellationToken).ConfigureAwait(false));

    [HttpPut("assessments/{id:guid}/grades")]
    [ProducesResponseType<GradesSaveResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GradesSaveResult>> SaveGrades(Guid id, GradesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        IReadOnlyList<GradeInput>? grades = request.Grades is null ? null : [.. request.Grades.Select(g => g?.ToInput()!)];
        return this.Ok(await this.sender.Send(new SaveGradesCommand(id, grades), cancellationToken).ConfigureAwait(false));
    }
}
