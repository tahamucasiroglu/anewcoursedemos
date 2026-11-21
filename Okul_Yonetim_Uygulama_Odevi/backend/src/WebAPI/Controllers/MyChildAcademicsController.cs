using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Academics.Records;
using OkulYonetim.WebAPI.Auth;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Velinin çocuk için ödev ve not özeti (spec 0013 gereksinim 7; okul seçilmiş olmalı). Veli rolü olmayan 403; bağı olmayan,
/// ayrılmış ya da başka okulun öğrencisi 404 — varlığı sızdırılmaz.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/my/children")]
public sealed class MyChildAcademicsController : ControllerBase
{
    private readonly ISender sender;

    public MyChildAcademicsController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet("{studentId:guid}/academics")]
    [ProducesResponseType<ChildAcademicsView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildAcademicsView>> Get(Guid studentId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetMyChildAcademicsQuery(studentId), cancellationToken).ConfigureAwait(false));
}
