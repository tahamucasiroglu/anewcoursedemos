using System.Collections.Concurrent;
using TahaKonsept.Modules.Agents.Application.Base;
using TahaKonsept.Modules.Agents.Application.Models;

namespace TahaKonsept.Modules.Agents.Application.Concrete;

/// <summary>
/// Bağlantıları süreç belleğinde (<see cref="ConcurrentDictionary{TKey,TValue}"/>) tutan izleyici. Tek API süreci olduğu için yeterlidir;
/// singleton kaydedilir. API yeniden başlarsa ajanlar otomatik yeniden bağlanır.
/// </summary>
public sealed class InMemoryAgentConnectionTracker : AgentConnectionTrackerBase
{
    private readonly ConcurrentDictionary<string, AgentConnection> _connections = new(StringComparer.Ordinal);

    /// <summary>Bağlantı ekler.</summary>
    /// <param name="connection">Bağlantı.</param>
    public override void Add(AgentConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connections[connection.ConnectionId] = connection;
    }

    /// <summary>Bağlantıyı çıkarır.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <returns>Çıkarılan bağlantı.</returns>
    public override AgentConnection? Remove(string connectionId) =>
        _connections.TryRemove(connectionId, out var connection) ? connection : null;

    /// <summary>Bağlantıyı getirir.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <returns>Bağlantı.</returns>
    public override AgentConnection? Get(string connectionId) =>
        _connections.TryGetValue(connectionId, out var connection) ? connection : null;

    /// <summary>Anlık kopya.</summary>
    /// <returns>Bağlantılar.</returns>
    protected override IReadOnlyCollection<AgentConnection> Snapshot() => _connections.Values.ToArray();
}
