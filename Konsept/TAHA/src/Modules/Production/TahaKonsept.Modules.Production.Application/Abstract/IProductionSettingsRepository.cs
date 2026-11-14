using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Üretim ayarları deposu.
/// </summary>
public interface IProductionSettingsRepository : IRepository<ProductionSettings>
{
    /// <summary>Kullanıcının kaydı (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<ProductionSettings?> GetForUserAsync(Guid userId, CancellationToken cancellationToken);
}
