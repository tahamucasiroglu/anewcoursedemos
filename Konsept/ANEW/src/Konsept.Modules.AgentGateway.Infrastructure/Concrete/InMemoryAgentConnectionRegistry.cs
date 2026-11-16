using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AgentGateway.Infrastructure.Base;

namespace Konsept.Modules.AgentGateway.Infrastructure.Concrete;

/// <summary>
/// Oturumları bellekte, kullanıcı başına tutar; API yeniden başlarsa ajan yeniden bağlanıp kendini yeniden kaydeder.
/// </summary>
public sealed class InMemoryAgentConnectionRegistry : AgentConnectionRegistryBase
{
    private readonly Dictionary<Guid, AgentSession> _sessions = [];

    /// <inheritdoc />
    protected override AgentSession? Read(Guid userId) => _sessions.GetValueOrDefault(userId);

    /// <inheritdoc />
    protected override IEnumerable<AgentSession> ReadAll() => _sessions.Values;

    /// <inheritdoc />
    protected override void Write(Guid userId, AgentSession? session)
    {
        if (session is null)
        {
            _sessions.Remove(userId);
        }
        else
        {
            _sessions[userId] = session;
        }
    }
}
