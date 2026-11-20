using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Administration.Branches;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Şube yönetimi (spec 0005): yalnızca seçili okulun kurucusu/yöneticisi (değilse 403). Başka okulun şubesi 404;
/// doğrulama hatası 400, iş kuralı (aynı ad, son etkin şube) 400 ve Türkçe mesaj.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantFounderPolicy)]
[Route("api/admin/branches")]
public sealed class AdminBranchesController : ControllerBase
{
    private readonly ISender sender;

    public AdminBranchesController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BranchAdminView>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BranchAdminView>>> List(CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ListAdminBranchesQuery(), cancellationToken).ConfigureAwait(false));

    [HttpPost]
    [ProducesResponseType<BranchAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BranchAdminView>> Create(BranchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new CreateBranchCommand(request.Name, request.Address, request.Phone), cancellationToken).ConfigureAwait(false));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<BranchAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BranchAdminView>> Update(Guid id, BranchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new UpdateBranchCommand(id, request.Name, request.Address, request.Phone), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType<BranchAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BranchAdminView>> Deactivate(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetBranchActiveCommand(id, false), cancellationToken).ConfigureAwait(false));

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType<BranchAdminView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BranchAdminView>> Activate(Guid id, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetBranchActiveCommand(id, true), cancellationToken).ConfigureAwait(false));
}
