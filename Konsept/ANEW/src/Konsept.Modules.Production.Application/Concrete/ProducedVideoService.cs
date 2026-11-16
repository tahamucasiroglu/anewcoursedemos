using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Base;
using Konsept.Modules.Profiles.Contracts.Abstract;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.Production.Application.Concrete;

/// <summary>Üretilen Videolar servisi (spec 0007); kurallar <see cref="ProducedVideoServiceBase"/>'de.</summary>
/// <param name="videos">Sürüm kayıtları.</param>
/// <param name="agents">Ajan komutları.</param>
/// <param name="profiles">Profil sahipliği.</param>
/// <param name="models">Katalog.</param>
/// <param name="user">İsteği yapan kullanıcı.</param>
public sealed class ProducedVideoService(
    IProducedVideoRepository videos,
    IAgentCommands agents,
    IProfileGenerationReader profiles,
    IModelDirectory models,
    ICurrentUser user)
    : ProducedVideoServiceBase(videos, agents, profiles, models, user);
