using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Base;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Production.Application.Concrete;

/// <summary>Üretim hattı (spec 0017); davranış <see cref="ProductionPipelineBase"/>'tedir. Koşu başına kendi DI kapsamında oluşturulur.</summary>
/// <param name="jobs">İş deposu.</param>
/// <param name="settingsStore">Ayar deposu.</param>
/// <param name="outputsStore">Çıktı deposu.</param>
/// <param name="videos">Video kayıtları.</param>
/// <param name="queue">Sıra durumu.</param>
/// <param name="concepts">Konsept köprüsü.</param>
/// <param name="profiles">Profil okuyucu.</param>
/// <param name="models">Model çağrısı.</param>
/// <param name="agents">Ajan komutları.</param>
/// <param name="components">Kurulu bileşenler.</param>
/// <param name="events">Günlük.</param>
/// <param name="time">Saat.</param>
/// <param name="logger">Teknik log.</param>
public sealed class ProductionPipeline(
    IProductionJobRepository jobs,
    IProductionSettingsRepository settingsStore,
    IStageOutputRepository outputsStore,
    IProducedVideoRepository videos,
    IQueueStateRepository queue,
    IConceptQueueBridge concepts,
    IProfileGenerationReader profiles,
    IModelCaller models,
    IAgentCommands agents,
    IInstalledComponentsReader components,
    ISystemEventWriter events,
    TimeProvider time,
    ILogger<ProductionPipeline> logger)
    : ProductionPipelineBase(jobs, settingsStore, outputsStore, videos, queue, concepts, profiles, models, agents, components, events, time, logger);
