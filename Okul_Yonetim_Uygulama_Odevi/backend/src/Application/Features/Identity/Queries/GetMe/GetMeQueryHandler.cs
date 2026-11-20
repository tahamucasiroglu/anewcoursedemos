using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Communication;
using OkulYonetim.Application.Features.Identity.Sessions;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Organization.Queries.GetBranches;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Identity.Queries.GetMe;

/// <summary>
/// Oturumdaki kişinin bilgisini toplar. Okul ve şubeler kurum/şube filtresinden geçer — seçili okul dışındaki hiçbir
/// okulun şubesi ya da rolü yanıta girmez; okul listesi yalnızca kişinin kendi atamalarından gelir.
/// </summary>
public sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, MeResponse>
{
    private readonly ICurrentSession currentSession;
    private readonly IUserRepository users;
    private readonly ITenantRepository tenants;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly UserTenantDirectory tenantDirectory;
    private readonly ISender sender;

    public GetMeQueryHandler(
        ICurrentSession currentSession,
        IUserRepository users,
        ITenantRepository tenants,
        IRoleAssignmentRepository roleAssignments,
        UserTenantDirectory tenantDirectory,
        ISender sender)
    {
        this.currentSession = currentSession;
        this.users = users;
        this.tenants = tenants;
        this.roleAssignments = roleAssignments;
        this.tenantDirectory = tenantDirectory;
        this.sender = sender;
    }

    public async Task<MeResponse> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        User user = this.currentSession.UserId is { } userId
            ? await this.users.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false) ?? throw new AuthorizationException()
            : throw new AuthorizationException();

        Tenant? tenant = await this.tenants.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<string> roles = [];
        IReadOnlyList<BranchSummary> branches = [];
        int unreadNotices = 0;
        if (tenant is not null)
        {
            IReadOnlyList<RoleAssignment> assignments = await this.roleAssignments.ListForUserInTenantAsync(user.Id, tenant.Id, cancellationToken).ConfigureAwait(false);
            roles = [.. assignments.Select(a => a.Role).Distinct().Order().Select(r => r.ToString())];
            branches = await this.sender.Send(new GetBranchesQuery(), cancellationToken).ConfigureAwait(false);
            unreadNotices = await this.sender.Send(new CountMyUnreadNoticesQuery(), cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<TenantSummary> all = await this.tenantDirectory.ListAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return new MeResponse(
            user.FullName,
            user.Email,
            tenant is null ? null : new TenantSummary(tenant.Id, tenant.Name),
            roles,
            branches,
            all,
            this.currentSession.IsPlatformAdmin,
            unreadNotices);
    }
}
