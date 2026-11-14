using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Api.Hubs.Base;

/// <summary>
/// İstemci vekil sağlayıcı tabanı: bağlantı kimliğini doğrular, vekili türetilen sınıftan (<see cref="ResolveClient"/>) alır.
/// </summary>
public abstract class AgentClientProviderBase : IAgentClientProvider
{
    /// <summary>Bağlantının istemci vekili.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <returns>Vekil.</returns>
    public IAgentClient GetClient(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        return ResolveClient(connectionId);
    }

    /// <summary>Vekili çözer.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <returns>Vekil.</returns>
    protected abstract IAgentClient ResolveClient(string connectionId);
}
