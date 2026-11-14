using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;

namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// Ajan olay işleyicisinin (<c>ProductionStageEventHandlerBase</c>) bağımlılık paketi (scoped).
/// </summary>
/// <param name="Jobs">İş deposu.</param>
/// <param name="Items">Sıra öğeleri deposu.</param>
/// <param name="Contexts">Bağlam fabrikası.</param>
/// <param name="Handlers">Aşama işleyicileri.</param>
/// <param name="Hasher">Hash'leyici.</param>
/// <param name="Finalizer">İş bitirici.</param>
/// <param name="Locks">Kullanıcı kilidi.</param>
/// <param name="Signal">Orkestratör sinyali.</param>
/// <param name="Activity">İşlem günlüğü.</param>
/// <param name="Clock">Saat.</param>
/// <param name="Logger">Günlük.</param>
public sealed record StageEventHandlerDependencies(
    IJobRepository Jobs,
    IQueueItemRepository Items,
    IPipelineContextFactory Contexts,
    IStageHandlerRegistry Handlers,
    IStageInputHasher Hasher,
    IJobFinalizer Finalizer,
    IUserJobLock Locks,
    IProductionSignal Signal,
    IProductionActivity Activity,
    IClock Clock,
    ILogger<IJobAdvancer> Logger);
