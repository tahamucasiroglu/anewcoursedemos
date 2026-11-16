using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Base;
using Konsept.Modules.Production.Application.Dtos;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.Production.Application.Concrete;

/// <summary>Konseptten Video servisi (spec 0006, 0017).</summary>
/// <param name="jobs">İş deposu.</param>
/// <param name="settingsStore">Ayar deposu.</param>
/// <param name="concepts">Konsept köprüsü.</param>
/// <param name="profiles">Profil okuyucu.</param>
/// <param name="models">Katalog dizini.</param>
/// <param name="components">Kurulu bileşenler.</param>
/// <param name="user">İsteği yapan kullanıcı.</param>
/// <param name="time">Saat.</param>
/// <param name="pipeline">Hat bağlantıları.</param>
public sealed class ProductionService(
    IProductionJobRepository jobs,
    IProductionSettingsRepository settingsStore,
    IConceptQueueBridge concepts,
    IProfileGenerationReader profiles,
    IModelDirectory models,
    IInstalledComponentsReader components,
    ICurrentUser user,
    TimeProvider time,
    ProductionPipelineLinks pipeline)
    : ProductionServiceBase(jobs, settingsStore, concepts, profiles, models, components, user, time, pipeline);
