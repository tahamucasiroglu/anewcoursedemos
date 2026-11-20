using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.GetMyChild;
using OkulYonetim.Application.Features.Students.Queries.ListMyChildren;
using OkulYonetim.WebAPI.Auth;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Velinin "Çocuklarım" uçları (spec 0009; okul seçilmiş olmalı): bağlı ve ayrılmamış çocukların listesi ve salt okunur
/// kart. Veli rolü olmayan kişi 403; bağı olmayan (aynı şubedeki bile) ya da ayrılmış öğrenci 404 — varlığı sızdırılmaz.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/my/children")]
public sealed class MyChildrenController : ControllerBase
{
    private readonly ISender sender;

    public MyChildrenController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ChildRow>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<ChildRow>>> List(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListMyChildrenQuery(), cancellationToken).ConfigureAwait(false));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ChildCard>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChildCard>> Get(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetMyChildQuery(id), cancellationToken).ConfigureAwait(false));
}
