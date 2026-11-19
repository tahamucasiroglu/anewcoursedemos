using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Identity.Sessions;

/// <summary>
/// Kişinin rolü olan okulları ada göre sıralı verir (giriş, okul seçimi, hoş geldin ekranı). Yalnızca o kişinin
/// atamalarından türetilir; başka kişinin okulu listeye giremez.
/// </summary>
public sealed class UserTenantDirectory
{
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly ITenantRepository tenants;

    public UserTenantDirectory(IRoleAssignmentRepository roleAssignments, ITenantRepository tenants)
    {
        this.roleAssignments = roleAssignments;
        this.tenants = tenants;
    }

    public async Task<IReadOnlyList<TenantSummary>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyList<RoleAssignment> assignments = await this.roleAssignments.ListForUserAsync(userId, cancellationToken).ConfigureAwait(false);
        Guid[] tenantIds = [.. assignments.Select(a => a.TenantId).Distinct()];
        if (tenantIds.Length == 0)
        {
            return [];
        }

        IReadOnlyList<Tenant> found = await this.tenants.ListByIdsAsync(tenantIds, cancellationToken).ConfigureAwait(false);
        return [.. found
            .OrderBy(t => t.Name, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true))
            .Select(t => new TenantSummary(t.Id, t.Name))];
    }
}
