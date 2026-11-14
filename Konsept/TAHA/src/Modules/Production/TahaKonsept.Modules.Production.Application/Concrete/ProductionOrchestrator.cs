using Microsoft.Extensions.Logging;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan orkestratör adımı (scoped; BackgroundService her turda yeni kapsamda çözer).
/// </summary>
/// <param name="queues">Sıra durumu deposu.</param>
/// <param name="items">Sıra öğeleri deposu.</param>
/// <param name="advancer">İş ilerletici.</param>
/// <param name="locks">Kullanıcı kilidi.</param>
/// <param name="logger">Günlük.</param>
public sealed class ProductionOrchestrator(
    IProductionQueueRepository queues,
    IQueueItemRepository items,
    IJobAdvancer advancer,
    IUserJobLock locks,
    ILogger<ProductionOrchestrator> logger)
    : ProductionOrchestratorBase(queues, items, advancer, locks, logger);
