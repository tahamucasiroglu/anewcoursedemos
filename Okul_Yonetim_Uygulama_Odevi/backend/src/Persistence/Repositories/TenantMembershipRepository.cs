using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Üyelik deposu: her okuma kurum filtresinden geçer (filtre dışı okuma yok).</summary>
public sealed class TenantMembershipRepository : ITenantMembershipRepository
{
    private readonly OkulDbContext context;

    public TenantMembershipRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<TenantMembership?> FindInCurrentTenantAsync(Guid userId, CancellationToken cancellationToken) =>
        this.context.Memberships.FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);

    public void Add(TenantMembership membership) => this.context.Memberships.Add(membership);
}
