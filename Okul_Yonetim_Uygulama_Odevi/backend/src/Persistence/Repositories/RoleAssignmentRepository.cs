using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Rol ataması deposu. İzinli liste (T5): iki okuma da filtre dışıdır ama yalnızca verilen kişinin atamalarını döndürür
/// (giriş anında henüz kurum yok; oturum doğrulanırken filtre değerleri bu okumayla belirlenir).
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
            .Where(r => r.UserId == userId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<RoleAssignment>> ListForUserInTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken) =>
        await this.context.RoleAssignments.IgnoreQueryFilters()
            .Where(r => r.UserId == userId && r.TenantId == tenantId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(RoleAssignment assignment) => this.context.RoleAssignments.Add(assignment);
}
