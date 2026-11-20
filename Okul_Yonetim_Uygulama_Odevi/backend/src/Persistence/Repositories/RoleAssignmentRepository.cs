using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Rol ataması deposu. İzinli liste (T5): iki okuma da filtre dışıdır ama yalnızca verilen kişinin atamalarını döndürür
/// (giriş anında henüz kurum yok; oturum doğrulanırken filtre değerleri bu okumayla belirlenir). Pasif üyelikteki
/// okulun atamaları dönmez (spec 0005: pasif kişi o okula giremez). Başka okul sayısı yalnızca sayı olarak döner.
/// </summary>
public sealed class RoleAssignmentRepository : IRoleAssignmentRepository
{
    private readonly OkulDbContext context;

    public RoleAssignmentRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<RoleAssignment>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await this.context.RoleAssignments.IgnoreQueryFilters()
            .Where(r => r.UserId == userId && this.context.Memberships.IgnoreQueryFilters()
                .Any(m => m.UserId == userId && m.TenantId == r.TenantId && m.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<RoleAssignment>> ListForUserInTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken) =>
        await this.context.RoleAssignments.IgnoreQueryFilters()
            .Where(r => r.UserId == userId && r.TenantId == tenantId && this.context.Memberships.IgnoreQueryFilters()
                .Any(m => m.UserId == userId && m.TenantId == tenantId && m.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<int> CountOtherTenantsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken) =>
        this.context.Memberships.IgnoreQueryFilters()
            .Where(m => m.UserId == userId && m.TenantId != tenantId)
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<RoleAssignment>> ListInCurrentTenantAsync(Guid userId, CancellationToken cancellationToken) =>
        await this.context.RoleAssignments.Where(r => r.UserId == userId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<int> CountActiveFoundersInCurrentTenantAsync(CancellationToken cancellationToken) =>
        this.context.RoleAssignments
            .Where(r => r.Role == Role.Founder && this.context.Memberships.Any(m => m.UserId == r.UserId && m.IsActive))
            .Select(r => r.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

    public void Add(RoleAssignment assignment) => this.context.RoleAssignments.Add(assignment);

    public void Remove(RoleAssignment assignment) => this.context.RoleAssignments.Remove(assignment);
}
