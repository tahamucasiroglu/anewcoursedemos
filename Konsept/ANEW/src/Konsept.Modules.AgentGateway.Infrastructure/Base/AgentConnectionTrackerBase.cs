using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Domain.Entities;

namespace Konsept.Modules.AgentGateway.Infrastructure.Base;

/// <summary>Açık bağlantı izleyicisinin kilitli ortak mantığı; saklama alt sınıftadır.</summary>
public abstract class AgentConnectionTrackerBase : IAgentConnectionTracker
{
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public void Track(string connectionId, AgentOwner owner, Action abort)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(abort);
        lock (_gate)
        {
            Save(connectionId, owner, abort);
        }
    }

    /// <inheritdoc />
    public void Untrack(string connectionId)
    {
        lock (_gate)
        {
            Delete(connectionId);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyValuePair<string, Action>> Take(Guid userId, IReadOnlyCollection<Guid> deviceIds)
    {
        ArgumentNullException.ThrowIfNull(deviceIds);
        lock (_gate)
        {
            var taken = Matching(userId, deviceIds).ToList();
            taken.ForEach(t => Delete(t.Key));
            return taken;
        }
    }

    /// <summary>Saklar (kilit altında çağrılır).</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="owner">Sahip.</param>
    /// <param name="abort">Kesme eylemi.</param>
    protected abstract void Save(string connectionId, AgentOwner owner, Action abort);

    /// <summary>Siler (kilit altında çağrılır).</summary>
    /// <param name="connectionId">Bağlantı.</param>
    protected abstract void Delete(string connectionId);

    /// <summary>Eşleşen bağlantılar (kilit altında çağrılır).</summary>
    /// <param name="userId">Sahip.</param>
    /// <param name="deviceIds">Cihazlar.</param>
    /// <returns>Bağlantılar ve kesme eylemleri.</returns>
    protected abstract IEnumerable<KeyValuePair<string, Action>> Matching(Guid userId, IReadOnlyCollection<Guid> deviceIds);
}
