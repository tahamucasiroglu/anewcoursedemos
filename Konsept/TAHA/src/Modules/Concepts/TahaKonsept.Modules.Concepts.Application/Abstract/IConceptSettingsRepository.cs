using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Domain.Entities;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Kullanıcı konsept ayarları deposu (kullanıcı başına tek kayıt).
/// </summary>
public interface IConceptSettingsRepository : IRepository<ConceptSettings>
{
    /// <summary>Kullanıcının ayarlarını takipli getirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayarlar ya da null.</returns>
    Task<ConceptSettings?> GetForUserTrackedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının ayarlarını takipsiz getirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayarlar ya da null.</returns>
    Task<ConceptSettings?> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
