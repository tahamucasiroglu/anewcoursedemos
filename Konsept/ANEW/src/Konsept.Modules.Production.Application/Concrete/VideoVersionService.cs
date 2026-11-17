using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Base;
using Konsept.Modules.Production.Application.Dtos;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.Production.Application.Concrete;

/// <summary>Yeni sürüm ve yeniden üret servisi (spec 0020); kurallar <see cref="VideoVersionServiceBase"/>'de.</summary>
/// <param name="videos">Sürüm kayıtları.</param>
/// <param name="jobs">İş deposu.</param>
/// <param name="outputs">Aşama çıktıları.</param>
/// <param name="concepts">Konsept köprüsü.</param>
/// <param name="regeneration">Yeniden üret bağlamı.</param>
/// <param name="models">Katalog.</param>
/// <param name="components">Kurulu bileşenler.</param>
/// <param name="production">Konseptten Video servisi.</param>
/// <param name="user">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
/// <param name="pipeline">Hat bağlantıları.</param>
public sealed class VideoVersionService(
    IProducedVideoRepository videos,
    IProductionJobRepository jobs,
    IStageOutputRepository outputs,
    IConceptQueueBridge concepts,
    IConceptRegenerationContext regeneration,
    IModelDirectory models,
    IInstalledComponentsReader components,
    IProductionService production,
    ICurrentUser user,
    TimeProvider time,
    ProductionPipelineLinks pipeline)
    : VideoVersionServiceBase(videos, jobs, outputs, concepts, regeneration, models, components, production, user, time, pipeline);
