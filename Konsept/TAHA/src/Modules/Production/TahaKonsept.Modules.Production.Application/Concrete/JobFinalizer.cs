using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan iş bitirici (scoped).
/// </summary>
/// <param name="videos">Video deposu.</param>
/// <param name="items">Sıra öğeleri deposu.</param>
/// <param name="artifacts">Artefakt deposu.</param>
/// <param name="activity">İşlem günlüğü.</param>
/// <param name="clock">Saat.</param>
/// <param name="logger">Günlük.</param>
/// <param name="rates">Konuşma hızı servisi.</param>
public sealed class JobFinalizer(
    IVideoRepository videos,
    IQueueItemRepository items,
    IArtifactStore artifacts,
    IProductionActivity activity,
    IClock clock,
    ILogger<JobFinalizer> logger,
    ISpeechRateService rates)
    : JobFinalizerBase(videos, items, artifacts, activity, clock, logger, rates);
