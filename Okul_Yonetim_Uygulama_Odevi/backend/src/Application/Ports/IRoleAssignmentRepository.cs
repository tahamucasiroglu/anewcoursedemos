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

    /// <summary>Kişinin bu okul dışında kaç okulda üyeliği var (yalnızca sayı; başka okulun hiçbir bilgisi dönmez).</summary>
    Task<int> CountOtherTenantsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Seçili okuldaki (filtreli) atamalar: kişinin bu okuldaki rolleri.</summary>
    Task<IReadOnlyList<RoleAssignment>> ListInCurrentTenantAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Seçili okulda etkin üyeliği olan kurucu/yönetici sayısı (spec 0005 "en az bir yönetici").</summary>
    Task<int> CountActiveFoundersInCurrentTenantAsync(CancellationToken cancellationToken);

    void Add(RoleAssignment assignment);

    void Remove(RoleAssignment assignment);
}
