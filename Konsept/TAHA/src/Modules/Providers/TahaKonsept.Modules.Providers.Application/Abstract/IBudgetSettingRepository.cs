using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Bütçe ayarı deposu.
/// </summary>
public interface IBudgetSettingRepository : IRepository<BudgetSetting>
{
    /// <summary>Kullanıcının ayarını (izlenen) bulur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<BudgetSetting?> FindByUserAsync(Guid userId, CancellationToken ct);
}
