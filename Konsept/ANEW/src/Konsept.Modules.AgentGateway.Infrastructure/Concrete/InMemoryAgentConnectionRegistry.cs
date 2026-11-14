using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AgentGateway.Infrastructure.Base;

namespace Konsept.Modules.AgentGateway.Infrastructure.Concrete;

/// <summary>
/// Oturumu bellekte tutar; API yeniden başlarsa ajan yeniden bağlanıp kendini yeniden kaydeder.
/// </summary>
public sealed class InMemoryAgentConnectionRegistry : AgentConnectionRegistryBase
{
    private AgentSession? _session;

    /// <inheritdoc />
    protected override AgentSession? Read() => _session;

    /// <inheritdoc />
    protected override void Write(AgentSession? session) => _session = session;
}
