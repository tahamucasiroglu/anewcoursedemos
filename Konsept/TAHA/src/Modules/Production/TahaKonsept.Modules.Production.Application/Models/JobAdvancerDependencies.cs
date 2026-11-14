using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;

namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// İş ilerleticinin (<c>JobAdvancerBase</c>) bağımlılık paketi (uzun kurucu listesi yerine; DI'da scoped kaydedilir).
/// </summary>
/// <param name="Queues">Sıra durumu deposu.</param>
/// <param name="Items">Sıra öğeleri deposu.</param>
/// <param name="Jobs">İş deposu.</param>
/// <param name="Factory">İş fabrikası.</param>
/// <param name="Contexts">Bağlam fabrikası.</param>
/// <param name="Handlers">Aşama işleyicileri.</param>
/// <param name="Hasher">Hash'leyici.</param>
/// <param name="Agent">Ajan geçidi.</param>
/// <param name="Budget">Bütçe bekçisi.</param>
/// <param name="Finalizer">İş bitirici.</param>
/// <param name="Activity">İşlem günlüğü.</param>
/// <param name="Cancellations">İş iptal kaydı (sıradan çıkarma zinciri anında durdurur).</param>
/// <param name="Clock">Saat.</param>
/// <param name="Logger">Günlük.</param>
public sealed record JobAdvancerDependencies(
    IProductionQueueRepository Queues,
    IQueueItemRepository Items,
    IJobRepository Jobs,
    IJobFactory Factory,
    IPipelineContextFactory Contexts,
    IStageHandlerRegistry Handlers,
    IStageInputHasher Hasher,
    IAgentGateway Agent,
    IBudgetGuard Budget,
    IJobFinalizer Finalizer,
    IProductionActivity Activity,
    IJobCancellationRegistry Cancellations,
    IClock Clock,
    ILogger<IJobAdvancer> Logger);
