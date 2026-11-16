using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.Production.Application.Abstract;

namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>
/// Konseptten Video servisinin üretim hattına bağlantıları (spec 0017; parametre nesnesi): koşucu, isteği yapanın model kimliği, ajan komutları,
/// video kayıtları ve sıra durumu.
/// </summary>
/// <param name="Runner">Koşucu.</param>
/// <param name="Caller">Model çağrısı (isteği yapanın kimliği).</param>
/// <param name="Agents">Ajan komutları.</param>
/// <param name="Videos">Video kayıtları.</param>
/// <param name="Queue">Sıra durumu.</param>
public sealed record ProductionPipelineLinks(
    IProductionRunner Runner, IModelCaller Caller, IAgentCommands Agents, IProducedVideoRepository Videos, IQueueStateRepository Queue);
