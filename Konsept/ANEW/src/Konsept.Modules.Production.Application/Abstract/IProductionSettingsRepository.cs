using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Modules.Production.Application.Abstract;

/// <summary>Profil başına üretim ayarlarının kalıcılığı (spec 0006 Req 7); kullanıcı kimliğiyle süzülür (BR-1).</summary>
public interface IProductionSettingsRepository
{
    /// <summary>Profilin kayıtlı ayarları.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayarlar ya da (hiç seçim yapılmadıysa) <c>null</c>.</returns>
    Task<ProductionSettings?> GetAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);

    /// <summary>Ekler ya da değiştirir.</summary>
    /// <param name="settings">Ayarlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task UpsertAsync(ProductionSettings settings, CancellationToken cancellationToken);
}
