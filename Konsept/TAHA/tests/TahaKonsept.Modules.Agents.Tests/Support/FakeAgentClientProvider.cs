using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Tests.Support;

/// <summary>
/// Her bağlantı için aynı <see cref="RecordingAgentClient"/>'ı veren sahte sağlayıcı; son istenen bağlantı kimliğini tutar.
/// </summary>
public sealed class FakeAgentClientProvider : IAgentClientProvider
{
    /// <summary>Kaydedici istemci.</summary>
    public RecordingAgentClient Client { get; } = new();

    /// <summary>Son istenen bağlantı.</summary>
    public string? LastConnectionId { get; private set; }

    /// <summary>İstemci verir.</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <returns>İstemci.</returns>
    public IAgentClient GetClient(string connectionId)
    {
        LastConnectionId = connectionId;
        return Client;
    }
}
