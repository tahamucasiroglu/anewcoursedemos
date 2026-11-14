using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Bir SignalR bağlantısına sunucu → ajan çağrıları yapan istemci vekilini verir. Api katmanında <c>IHubContext&lt;AgentHub, IAgentClient&gt;</c>
/// ile uygulanır; testlerde sahte uygulama kullanılır.
/// </summary>
public interface IAgentClientProvider
{
    /// <summary>Bağlantının istemci vekili.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <returns>İstemci vekili.</returns>
    IAgentClient GetClient(string connectionId);
}
