using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Contracts.Abstract;

/// <summary>
/// Ajanın anlık durumunu diğer modüllere açar (ör. sistem durumu).
/// </summary>
public interface IAgentStatusReader
{
    /// <summary>Anlık ajan durumunu döndürür.</summary>
    /// <returns>Ajan durumu.</returns>
    AgentStatusDto GetStatus();
}
