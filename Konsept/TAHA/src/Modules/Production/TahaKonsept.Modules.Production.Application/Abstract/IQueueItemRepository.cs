using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Sıra öğeleri deposu (kullanıcıya göre filtreli).
/// </summary>
public interface IQueueItemRepository : IRepository<QueueItem>
{
    /// <summary>Kullanıcının öğeleri sırayla (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<IReadOnlyList<QueueItem>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Kullanıcının öğesi (takipli).</summary>
    /// <param name="id">Öğe.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<QueueItem?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>Çalışan/duraklatılmış öğe (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<QueueItem?> GetActiveAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Sıradaki bekleyen öğe (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<QueueItem?> GetNextWaitingAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>En büyük sıra numarası (yoksa 0).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<int> MaxOrderAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Etkin öğesi olan kullanıcılar.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<IReadOnlyList<Guid>> ListUserIdsWithActiveItemsAsync(CancellationToken cancellationToken);
}
