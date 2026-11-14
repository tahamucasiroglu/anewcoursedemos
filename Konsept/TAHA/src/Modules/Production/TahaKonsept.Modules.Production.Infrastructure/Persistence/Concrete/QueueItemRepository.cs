using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IQueueItemRepository"/>'nin EF Core uygulaması: kullanıcıya göre sıralı öğeler, etkin öğe ve sıradaki bekleyen öğe.
/// </summary>
/// <param name="context">Modül DbContext'i.</param>
public sealed class QueueItemRepository(ProductionDbContext context) : Repository<QueueItem>(context), IQueueItemRepository
{
    /// <summary>Kullanıcının öğeleri sırayla (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Öğeler.</returns>
    public async Task<IReadOnlyList<QueueItem>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await Query().Where(x => x.UserId == userId).OrderBy(x => x.Order).ThenBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

    /// <summary>Kullanıcının öğesi (takipli).</summary>
    /// <param name="id">Öğe.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Öğe ya da <c>null</c>.</returns>
    public Task<QueueItem?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        Query().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

    /// <summary>Çalışan/duraklatılmış öğe (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Öğe ya da <c>null</c>.</returns>
    public Task<QueueItem?> GetActiveAsync(Guid userId, CancellationToken cancellationToken) =>
        Query().Where(x => x.UserId == userId && (x.Status == QueueItemStatus.Running || x.Status == QueueItemStatus.Paused))
            .OrderBy(x => x.Order).FirstOrDefaultAsync(cancellationToken);

    /// <summary>Sıradaki bekleyen öğe (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Öğe ya da <c>null</c>.</returns>
    public Task<QueueItem?> GetNextWaitingAsync(Guid userId, CancellationToken cancellationToken) =>
        Query().Where(x => x.UserId == userId && x.Status == QueueItemStatus.Waiting)
            .OrderBy(x => x.Order).ThenBy(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);

    /// <summary>En büyük sıra numarası.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sıra (yoksa 0).</returns>
    public async Task<int> MaxOrderAsync(Guid userId, CancellationToken cancellationToken) =>
        await QueryNoTracking().Where(x => x.UserId == userId).Select(x => (int?)x.Order).MaxAsync(cancellationToken) ?? 0;

    /// <summary>Konseptin hata/iptal ile bitmemiş öğelerindeki en büyük sürüm.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sürüm (yoksa 0).</returns>
    public async Task<int> MaxVersionForConceptAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken) =>
        await QueryNoTracking()
            .Where(x => x.UserId == userId && x.ConceptId == conceptId && x.Status != QueueItemStatus.Failed && x.Status != QueueItemStatus.Cancelled)
            .Select(x => (int?)x.Version)
            .MaxAsync(cancellationToken) ?? 0;

    /// <summary>Etkin öğesi olan kullanıcılar.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    public async Task<IReadOnlyList<Guid>> ListUserIdsWithActiveItemsAsync(CancellationToken cancellationToken) =>
        await QueryNoTracking().Where(x => x.Status == QueueItemStatus.Running || x.Status == QueueItemStatus.Paused)
            .Select(x => x.UserId).Distinct().ToListAsync(cancellationToken);
}
