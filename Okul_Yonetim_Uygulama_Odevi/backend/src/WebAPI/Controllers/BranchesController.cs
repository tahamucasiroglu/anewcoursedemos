using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Organization.Queries.GetBranchById;
using OkulYonetim.Application.Features.Organization.Queries.GetBranches;
using OkulYonetim.WebAPI.Auth;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Şube uçları (okul seçilmiş olmalı): görünür şubeler ve tek şube. Başka okulun ya da yetkisiz şubenin kaydı 404 —
/// varlığı sızdırılmaz (S1).
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantSelectedPolicy)]
[Route("api/branches")]
public sealed class BranchesController : ControllerBase
{
    private readonly ISender sender;

    public BranchesController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BranchSummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BranchSummary>>> List(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetBranchesQuery(), cancellationToken).ConfigureAwait(false));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<BranchSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BranchSummary>> Get(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetBranchByIdQuery(id), cancellationToken).ConfigureAwait(false));
}
