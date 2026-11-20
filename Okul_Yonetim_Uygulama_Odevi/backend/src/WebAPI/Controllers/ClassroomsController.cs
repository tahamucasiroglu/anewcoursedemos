using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Organization.Queries.GetClassroomById;
using OkulYonetim.Application.Features.Organization.Queries.ListClassrooms;
using OkulYonetim.WebAPI.Auth;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Sınıf okuma uçları (spec 0008; okul seçilmiş olmalı): kişinin görebildiği etkin sınıflar ve tek sınıf. Atanmadığı ya da
/// pasif şubenin sınıfı ve başka okulun sınıfı 404 — varlığı sızdırılmaz (S1).
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/classrooms")]
public sealed class ClassroomsController : ControllerBase
{
    private readonly ISender sender;

    public ClassroomsController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ClassroomSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ClassroomSummary>>> List([FromQuery] Guid? termId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListClassroomsQuery(termId), cancellationToken).ConfigureAwait(false));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ClassroomSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassroomSummary>> Get(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetClassroomByIdQuery(id), cancellationToken).ConfigureAwait(false));
}
