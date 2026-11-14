using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Sıra çalışma durumu deposu.
/// </summary>
public interface IProductionQueueRepository : IRepository<ProductionQueue>
{
    /// <summary>Kullanıcının sıra kaydı (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<ProductionQueue?> GetForUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Sırası çalışan ya da duraklatılmış kullanıcılar.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<IReadOnlyList<Guid>> ListNonIdleUserIdsAsync(CancellationToken cancellationToken);
}
