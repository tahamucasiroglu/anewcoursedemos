using Microsoft.AspNetCore.SignalR;
using TahaKonsept.Modules.Agents.Api.Hubs.Base;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Api.Hubs.Concrete;

/// <summary>
/// <see cref="IHubContext{THub, T}"/> üzerinden ajan bağlantısına tip güvenli istemci vekili verir (singleton).
/// </summary>
public sealed class SignalRAgentClientProvider : AgentClientProviderBase
{
    private readonly IHubContext<AgentHub, IAgentClient> _hub;

    /// <summary>Oluşturur.</summary>
    /// <param name="hub">Hub bağlamı.</param>
    public SignalRAgentClientProvider(IHubContext<AgentHub, IAgentClient> hub)
    {
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));
    }

    /// <summary>Vekili çözer.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <returns>Vekil.</returns>
    protected override IAgentClient ResolveClient(string connectionId) => _hub.Clients.Client(connectionId);
}
