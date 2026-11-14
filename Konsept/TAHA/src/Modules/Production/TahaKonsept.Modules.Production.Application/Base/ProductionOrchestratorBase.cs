using Microsoft.Extensions.Logging;
using TahaKonsept.Modules.Production.Application.Abstract;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IProductionOrchestrator"/> ortak davranışı: sırası Boşta olmayan ya da etkin öğesi olan kullanıcıları bulur (<see cref="ListUsersAsync"/>);
/// bir kullanıcı için kilidi alıp <see cref="IJobAdvancer.AdvanceUserAsync"/> çağırır (<see cref="AdvanceAsync"/>). Bir kullanıcının hatası diğerlerini
/// durdurmaz (Error günlüğü); sıradan çıkarma ile kesilen zincir (iptal) Warning olarak yazılır, döngüyü bozmaz.
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

    /// <summary>İlerletilecek kullanıcılar.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    public async Task<IReadOnlyList<Guid>> ListUsersAsync(CancellationToken cancellationToken) =>
        (await _queues.ListNonIdleUserIdsAsync(cancellationToken))
            .Concat(await _items.ListUserIdsWithActiveItemsAsync(cancellationToken))
            .Distinct()
            .ToList();

    /// <summary>Tek kullanıcıyı ilerletir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public async Task AdvanceAsync(Guid userId, CancellationToken cancellationToken)
    {
        using var held = await _locks.AcquireAsync(userId, cancellationToken);
        try
        {
            await _advancer.AdvanceUserAsync(userId, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Üretim zinciri kesildi (sıradan çıkarma ya da iptal edilen çağrı; kullanıcı {UserId})", userId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Üretim sırası ilerletilemedi (kullanıcı {UserId})", userId);
        }
    }

    /// <summary>Bir tur çalıştırır (kullanıcılar sırayla).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İşlenen kullanıcı sayısı.</returns>
    public async Task<int> TickAsync(CancellationToken cancellationToken)
    {
        var users = await ListUsersAsync(cancellationToken);
        foreach (var userId in users)
        {
            await AdvanceAsync(userId, cancellationToken);
        }

        return users.Count;
    }
}
