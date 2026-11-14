using Microsoft.Extensions.Logging;
using TahaKonsept.Modules.Production.Application.Abstract;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IProductionOrchestrator"/> ortak davranışı: sırası Boşta olmayan ya da etkin öğesi olan kullanıcıları bulur; her biri için kullanıcı
/// kilidini alıp <see cref="IJobAdvancer.AdvanceUserAsync"/> çağırır. Bir kullanıcının hatası diğerlerini durdurmaz (Error günlüğü).
/// </summary>
public abstract class ProductionOrchestratorBase : IProductionOrchestrator
{
    private readonly IProductionQueueRepository _queues;
    private readonly IQueueItemRepository _items;
    private readonly IJobAdvancer _advancer;
    private readonly IUserJobLock _locks;
    private readonly ILogger _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="queues">Sıra durumu deposu.</param>
    /// <param name="items">Sıra öğeleri deposu.</param>
    /// <param name="advancer">İş ilerletici.</param>
    /// <param name="locks">Kullanıcı kilidi.</param>
    /// <param name="logger">Günlük.</param>
    protected ProductionOrchestratorBase(IProductionQueueRepository queues, IQueueItemRepository items, IJobAdvancer advancer, IUserJobLock locks, ILogger logger)
    {
        _queues = queues ?? throw new ArgumentNullException(nameof(queues));
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _advancer = advancer ?? throw new ArgumentNullException(nameof(advancer));
        _locks = locks ?? throw new ArgumentNullException(nameof(locks));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Bir tur çalıştırır.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İşlenen kullanıcı sayısı.</returns>
    public async Task<int> TickAsync(CancellationToken cancellationToken)
    {
        var users = (await _queues.ListNonIdleUserIdsAsync(cancellationToken))
            .Concat(await _items.ListUserIdsWithActiveItemsAsync(cancellationToken))
            .Distinct()
            .ToList();
        foreach (var userId in users)
        {
            using var held = await _locks.AcquireAsync(userId, cancellationToken);
            try
            {
                await _advancer.AdvanceUserAsync(userId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Üretim sırası ilerletilemedi (kullanıcı {UserId})", userId);
            }
        }

        return users.Count;
    }
}
