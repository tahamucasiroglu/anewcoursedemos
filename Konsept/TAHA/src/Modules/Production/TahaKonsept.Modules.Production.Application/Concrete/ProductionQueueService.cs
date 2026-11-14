using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan üretim sırası servisi (scoped).
/// </summary>
/// <param name="queues">Sıra durumu deposu.</param>
/// <param name="items">Sıra öğeleri deposu.</param>
/// <param name="jobs">İş deposu.</param>
/// <param name="concepts">Konsept portu.</param>
/// <param name="agent">Ajan geçidi.</param>
/// <param name="artifacts">Artefakt deposu.</param>
/// <param name="locks">Kullanıcı kilidi.</param>
/// <param name="signal">Orkestratör sinyali.</param>
/// <param name="currentUser">Oturumdaki kullanıcı.</param>
/// <param name="clock">Saat.</param>
public sealed class ProductionQueueService(
    IProductionQueueRepository queues,
    IQueueItemRepository items,
    IJobRepository jobs,
    IConceptSource concepts,
    IAgentGateway agent,
    IArtifactStore artifacts,
    IUserJobLock locks,
    IProductionSignal signal,
    ICurrentUser currentUser,
    IClock clock)
    : ProductionQueueServiceBase(queues, items, jobs, concepts, agent, artifacts, locks, signal, currentUser, clock);
