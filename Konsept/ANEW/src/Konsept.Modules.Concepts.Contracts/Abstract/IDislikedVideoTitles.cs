using Konsept.Modules.Concepts.Contracts.Dtos;

namespace Konsept.Modules.Concepts.Contracts.Abstract;

/// <summary>
/// Silinenler'deki videoların başlıkları (spec 0019 Req 6, E-2): konu önerisinde "beğenilmedi" negatif sinyali. Production uygular; kullanıcı
/// ve profil kimliğiyle süzülür (BR-1). Kalıcı silinen video listede yoktur.
/// </summary>
public interface IDislikedVideoTitles
{
    /// <summary>Profilin Silinenler'deki videoları.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Başlık ve silinme anı.</returns>
    Task<IReadOnlyList<DislikedVideoTitle>> ListAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);
}
