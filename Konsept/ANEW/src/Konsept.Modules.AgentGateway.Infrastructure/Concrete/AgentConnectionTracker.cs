using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AgentGateway.Infrastructure.Base;

namespace Konsept.Modules.AgentGateway.Infrastructure.Concrete;

/// <summary>Açık hub bağlantılarını bellekte izler (tek Api süreci).</summary>
public sealed class AgentConnectionTracker : AgentConnectionTrackerBase
{
    private readonly Dictionary<string, (AgentOwner Owner, Action Abort)> _connections = new(StringComparer.Ordinal);

    /// <inheritdoc />
    protected override void Save(string connectionId, AgentOwner owner, Action abort) => _connections[connectionId] = (owner, abort);

    /// <inheritdoc />
    protected override bool Exists(string connectionId) => _connections.ContainsKey(connectionId);

    /// <inheritdoc />
    protected override void Delete(string connectionId) => _connections.Remove(connectionId);

    /// <inheritdoc />
    protected override IEnumerable<KeyValuePair<string, Action>> Matching(Guid userId, IReadOnlyCollection<Guid> deviceIds) =>
        _connections
            .Where(c => c.Value.Owner.UserId == userId && deviceIds.Contains(c.Value.Owner.DeviceId))
            .Select(c => new KeyValuePair<string, Action>(c.Key, c.Value.Abort));
}
