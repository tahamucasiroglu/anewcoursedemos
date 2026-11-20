using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Communication.Announcements;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Duyuru uçları (spec 0011; okul seçilmiş olmalı): kurucu/öğretmen listesi, yayın ve formun kapsam seçenekleri (sınıflar,
/// sınıfın öğrencileri), velinin akışı, ayrıntı (veli açınca
/// okundu), sonuçlar (yalnızca yayınlayan ve kurucu) ve onay cevabı (veli). Rol ve ilişki handler'da: rol yoksa 403, ilişkisi
/// olmayan kayıt 404; alan hatası ve iş kuralı ihlali 400.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/announcements")]
public sealed class AnnouncementsController : ControllerBase
{
    private readonly ISender sender;

    public AnnouncementsController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StaffAnnouncementRow>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<StaffAnnouncementRow>>> List(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListStaffAnnouncementsQuery(), cancellationToken).ConfigureAwait(false));

    [HttpPost]
    [ProducesResponseType<AnnouncementPublished>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnnouncementPublished>> Publish(PublishAnnouncementRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(
            new PublishAnnouncementCommand(request.Title, request.Body, request.Scope, request.ScopeId, request.RequestConsent, request.ConsentQuestion, request.ConsentDeadline),
            cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("feed")]
    [ProducesResponseType<FeedPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<FeedPage>> Feed([FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        this.Ok(await this.sender.Send(new ListMyFeedQuery(page), cancellationToken).ConfigureAwait(false));

    [HttpGet("scope-classrooms")]
    [ProducesResponseType<IReadOnlyList<ScopeClassroom>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ScopeClassroom>>> ScopeClassrooms(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListScopeClassroomsQuery(), cancellationToken).ConfigureAwait(false));

    [HttpGet("scope-students")]
    [ProducesResponseType<IReadOnlyList<ScopeStudent>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ScopeStudent>>> ScopeStudents([FromQuery] Guid classroomId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListScopeStudentsQuery(classroomId), cancellationToken).ConfigureAwait(false));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AnnouncementDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnnouncementDetail>> Get(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetAnnouncementQuery(id), cancellationToken).ConfigureAwait(false));

    [HttpGet("{id:guid}/results")]
    [ProducesResponseType<AnnouncementResults>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnnouncementResults>> Results(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetAnnouncementResultsQuery(id), cancellationToken).ConfigureAwait(false));

    [HttpPost("{id:guid}/consent")]
    [ProducesResponseType<ConsentChildView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsentChildView>> Consent(Guid id, ConsentAnswerRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new AnswerConsentCommand(id, request.StudentId, request.Answer), cancellationToken).ConfigureAwait(false));
    }
}
