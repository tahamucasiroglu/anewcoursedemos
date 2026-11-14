using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Domain.Entities;

namespace Konsept.Modules.AgentGateway.Infrastructure.Base;

/// <summary>
/// Oturum kaydının iş parçacığı güvenli ortak mantığı; saklama biçimini alt sınıfa bırakır.
/// </summary>
public abstract class AgentConnectionRegistryBase : IAgentConnectionRegistry
{
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public AgentSession? Current
    {
        get
        {
            lock (_gate)
            {
                return Read();
            }
        }
    }

    /// <inheritdoc />
    public void Set(AgentSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        lock (_gate)
        {
            Write(session);
        }
    }

    /// <inheritdoc />
    public AgentSession? Remove(string connectionId)
    {
        lock (_gate)
        {
            var current = Read();
            if (current is null || !string.Equals(current.ConnectionId, connectionId, StringComparison.Ordinal))
            {
                return null;
            }

            Write(null);
            return current;
        }
    }

    /// <summary>Saklanan oturumu okur (kilit altında çağrılır).</summary>
    /// <returns>Oturum ya da <c>null</c>.</returns>
    protected abstract AgentSession? Read();

    /// <summary>Oturumu saklar (kilit altında çağrılır).</summary>
    /// <param name="session">Oturum ya da <c>null</c>.</param>
    protected abstract void Write(AgentSession? session);
}
