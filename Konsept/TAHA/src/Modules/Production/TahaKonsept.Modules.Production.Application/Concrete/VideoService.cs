using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan üretilen videolar servisi (scoped).
/// </summary>
/// <param name="videos">Video deposu.</param>
/// <param name="items">Sıra öğeleri deposu.</param>
/// <param name="queues">Sıra durumu deposu.</param>
/// <param name="settings">Ayar servisi.</param>
/// <param name="agent">Ajan geçidi.</param>
/// <param name="signal">Orkestratör sinyali.</param>
/// <param name="activity">İşlem günlüğü.</param>
/// <param name="currentUser">Oturumdaki kullanıcı.</param>
/// <param name="logger">Günlük.</param>
public sealed class VideoService(
    IVideoRepository videos,
    IQueueItemRepository items,
    IProductionQueueRepository queues,
    IProductionSettingsService settings,
    IAgentGateway agent,
    IProductionSignal signal,
    IProductionActivity activity,
    ICurrentUser currentUser,
    ILogger<VideoService> logger)
    : VideoServiceBase(videos, items, queues, settings, agent, signal, activity, currentUser, logger);
