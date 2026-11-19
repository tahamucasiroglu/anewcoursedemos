using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Rol atamaları. İki okuma kurum filtresi dışındadır ve izinli listededir (T5), çünkü henüz kurum seçilmemişken ya da
/// oturum doğrulanırken gerekir; ikisi de yalnızca verilen kişinin atamalarını döndürür.
/// </summary>
public interface IRoleAssignmentRepository
{
    Task<IReadOnlyList<RoleAssignment>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleAssignment>> ListForUserInTenantAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);

    void Add(RoleAssignment assignment);
}
