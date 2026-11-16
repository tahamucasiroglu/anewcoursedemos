using Konsept.Modules.Profiles.Contracts.Dtos;

namespace Konsept.Modules.Profiles.Contracts.Abstract;

/// <summary>
/// Konsept üretimi için profil ve konsept ayarları (spec 0014). Kullanıcı kimliğiyle süzülür (BR-1): başka kullanıcının profili
/// <c>null</c>'dır. İstek bağlamı gerektirmez (arka plan işinde de kullanılabilir).
/// </summary>
public interface IProfileGenerationReader
{
    /// <summary>Profilin üretim bağlamı.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Bağlam; profil yoksa ya da başkasınınsa <c>null</c>.</returns>
    Task<ProfileGenerationContext?> GetAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);
}
