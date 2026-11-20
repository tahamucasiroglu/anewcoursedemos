using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Kurum deposu. <c>GetCurrentAsync</c> filtrelidir. İzinli liste (T5): <c>ListByIdsAsync</c> yalnızca çağıranın
/// kişinin kendi atamalarından ürettiği kimlikleri okur; <c>FindFoundedByNameAsync</c> yalnızca verilen kişinin kurucusu
/// olduğu kurumu arar — ikisi de kurum seçilmeden önce çalışmak zorunda olduğu için filtre dışıdır.
/// </summary>
public sealed class TenantRepository : ITenantRepository
{
    private readonly OkulDbContext context;

    public TenantRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<Tenant?> GetCurrentAsync(CancellationToken cancellationToken) =>
        this.context.Tenants.FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Tenant>> ListByIdsAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken cancellationToken) =>
        await this.context.Tenants.IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<Tenant?> FindFoundedByNameAsync(Guid founderUserId, string name, CancellationToken cancellationToken)
    {
        string pattern = LikePattern.Escape(name);
        return this.context.Tenants.IgnoreQueryFilters()
            .Where(t => EF.Functions.ILike(t.Name, pattern))
            .Where(t => this.context.RoleAssignments.IgnoreQueryFilters()
                .Any(r => r.TenantId == t.Id && r.UserId == founderUserId && r.Role == Role.Founder))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(Tenant tenant) => this.context.Tenants.Add(tenant);

}
