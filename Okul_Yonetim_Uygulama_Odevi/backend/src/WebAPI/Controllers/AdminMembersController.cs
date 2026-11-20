using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Administration.Members;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Controllers.Contracts;

namespace OkulYonetim.WebAPI.Controllers;

/// <summary>
/// Kişi yönetimi (spec 0005): liste, kart, ekleme, profil, roller, pasifleştirme ve şifre sıfırlama. Yalnızca seçili
/// okulun kurucusu/yöneticisi (değilse 403); kişi bu okulda değilse 404. Geçici şifre yalnızca ekleme ve sıfırlama
/// yanıtında, bir kez döner.
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = AuthSetup.TenantFounderPolicy)]
[Route("api/admin/members")]
public sealed class AdminMembersController : ControllerBase
{
    private readonly ISender sender;

    public AdminMembersController(ISender sender)
    {
        this.sender = sender;
    }

    [HttpGet]
    [ProducesResponseType<MemberPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberPage>> List(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] Guid? branchId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default) =>
        this.Ok(await this.sender.Send(new ListMembersQuery(search, role, branchId, status, page), cancellationToken).ConfigureAwait(false));

    [HttpGet("{userId:guid}")]
    [ProducesResponseType<MemberDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetail>> Get(Guid userId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new GetMemberQuery(userId), cancellationToken).ConfigureAwait(false));

    [HttpPost]
    [ProducesResponseType<AddMemberResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddMemberResult>> Add(AddMemberRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new AddMemberCommand(request.FullName, request.Email, request.Phone, request.Roles ?? []), cancellationToken).ConfigureAwait(false));
    }

    [HttpPut("{userId:guid}/profile")]
    [ProducesResponseType<MemberDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetail>> UpdateProfile(Guid userId, MemberProfileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new UpdateMemberProfileCommand(userId, request.FullName, request.Phone), cancellationToken).ConfigureAwait(false));
    }

    [HttpPut("{userId:guid}/roles")]
    [ProducesResponseType<MemberDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetail>> SetRoles(Guid userId, MemberRolesRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.Ok(await this.sender.Send(new SetMemberRolesCommand(userId, request.Roles ?? []), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("{userId:guid}/deactivate")]
    [ProducesResponseType<MemberDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetail>> Deactivate(Guid userId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetMemberActiveCommand(userId, false), cancellationToken).ConfigureAwait(false));

    [HttpPost("{userId:guid}/activate")]
    [ProducesResponseType<MemberDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDetail>> Activate(Guid userId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new SetMemberActiveCommand(userId, true), cancellationToken).ConfigureAwait(false));

    [HttpPost("{userId:guid}/reset-password")]
    [ProducesResponseType<ResetMemberPasswordResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResetMemberPasswordResult>> ResetPassword(Guid userId, CancellationToken cancellationToken) =>
        this.Ok(await this.sender.Send(new ResetMemberPasswordCommand(userId), cancellationToken).ConfigureAwait(false));
}
