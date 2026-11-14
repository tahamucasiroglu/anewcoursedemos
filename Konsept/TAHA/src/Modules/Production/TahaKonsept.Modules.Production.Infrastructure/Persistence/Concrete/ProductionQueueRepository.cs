using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IProductionQueueRepository"/>'nin EF Core uygulaması: kullanıcı başına sıra durumu ve orkestratör için Boşta olmayan kullanıcılar.
/// </summary>
/// <param name="context">Modül DbContext'i.</param>
public sealed class ProductionQueueRepository(ProductionDbContext context) : Repository<ProductionQueue>(context), IProductionQueueRepository
{
    /// <summary>Kullanıcının sıra kaydı (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıt ya da <c>null</c>.</returns>
    public Task<ProductionQueue?> GetForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Query().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    /// <summary>Sırası çalışan ya da duraklatılmış kullanıcılar.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    public async Task<IReadOnlyList<Guid>> ListNonIdleUserIdsAsync(CancellationToken cancellationToken) =>
        await QueryNoTracking().Where(x => x.RunState != QueueRunState.Idle).Select(x => x.UserId).Distinct().ToListAsync(cancellationToken);

    /// <summary>Sıra durumunu takipsiz okur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum ya da <c>null</c>.</returns>
    public async Task<QueueRunState?> GetRunStateAsync(Guid userId, CancellationToken cancellationToken) =>
        await QueryNoTracking().Where(x => x.UserId == userId).Select(x => (QueueRunState?)x.RunState).FirstOrDefaultAsync(cancellationToken);
}
