using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Üretim ayarları servisi (<c>GET|PUT api/production/settings</c>) ve arka plan için kullanıcı ayarı okuma (<see cref="GetForUserAsync"/>; kayıt yoksa varsayılanlar).
/// </summary>
public interface IProductionSettingsService
{
    /// <summary>Oturumdaki kullanıcının ayarları.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<ProductionSettingsDto>> GetAsync(CancellationToken cancellationToken);

    /// <summary>Ayarları kaydeder.</summary>
    /// <param name="settings">Ayarlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<ProductionSettingsDto>> UpdateAsync(ProductionSettingsDto settings, CancellationToken cancellationToken);

    /// <summary>Kullanıcının ayarları (varsayılanlarla).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<ProductionSettingsDto> GetForUserAsync(Guid userId, CancellationToken cancellationToken);
}
