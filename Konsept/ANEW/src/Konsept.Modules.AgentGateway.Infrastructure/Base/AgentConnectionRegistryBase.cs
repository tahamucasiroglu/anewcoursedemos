using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Domain.Entities;

namespace Konsept.Modules.AgentGateway.Infrastructure.Base;

/// <summary>
/// Oturum kaydının iş parçacığı güvenli ortak mantığı (kullanıcı başına tek oturum); saklama biçimini alt sınıfa bırakır.
/// </summary>
public abstract class AgentConnectionRegistryBase : IAgentConnectionRegistry
{
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public AgentSession? Find(Guid userId)
    {
        lock (_gate)
        {
            return Read(userId);
        }
    }

    /// <inheritdoc />
    public AgentSession? FindByConnection(string connectionId)
    {
        lock (_gate)
        {
            return ReadAll().FirstOrDefault(s => string.Equals(s.ConnectionId, connectionId, StringComparison.Ordinal));
        }
    }

    /// <inheritdoc />
    public void Set(AgentSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        lock (_gate)
        {
            Write(session.UserId, session);
        }
    }

    /// <inheritdoc />
    public AgentSession? Remove(string connectionId)
    {
        lock (_gate)
        {
            var current = ReadAll().FirstOrDefault(s => string.Equals(s.ConnectionId, connectionId, StringComparison.Ordinal));
            if (current is null)
            {
                return null;
            }

            Write(current.UserId, null);
            return current;
        }
    }

    /// <summary>Kullanıcının oturumunu okur (kilit altında çağrılır).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Oturum ya da <c>null</c>.</returns>
    protected abstract AgentSession? Read(Guid userId);

    /// <summary>Tüm oturumları okur (kilit altında çağrılır).</summary>
    /// <returns>Oturumlar.</returns>
    protected abstract IEnumerable<AgentSession> ReadAll();

    /// <summary>Kullanıcının oturumunu saklar ya da siler (kilit altında çağrılır).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="session">Oturum ya da <c>null</c>.</param>
    protected abstract void Write(Guid userId, AgentSession? session);
}
