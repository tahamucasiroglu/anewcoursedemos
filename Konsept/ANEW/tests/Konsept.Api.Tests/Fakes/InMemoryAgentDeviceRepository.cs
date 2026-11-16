using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Hızlı profil için bellekte ajan cihaz deposu (spec 0012); her işlem tek kilit altında (gerçek deponun tek ifadelik
/// <c>UPDATE</c>'lerinin eşdeğeri).
/// </summary>
public sealed class InMemoryAgentDeviceRepository : IAgentDeviceRepository
{
    private readonly Lock _gate = new();
    private readonly List<AgentDevice> _devices = [];

    /// <summary>Gets cihazların anlık kopyası.</summary>
    public IReadOnlyList<AgentDevice> Devices
    {
        get
        {
            lock (_gate)
            {
                return [.. _devices];
            }
        }
    }

    /// <inheritdoc />
    public Task AddAsync(AgentDevice device, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _devices.Add(device);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<AgentDevice?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_devices.FirstOrDefault(d => d.TokenHash == tokenHash));
        }
    }

    /// <inheritdoc />
    public Task TouchAsync(Guid deviceId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _devices.FirstOrDefault(d => d.Id == deviceId && d.RevokedAt is null)?.Touch(now);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Guid>> RevokeActiveAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var active = _devices.Where(d => d.UserId == userId && d.RevokedAt is null).ToList();
            active.ForEach(d => d.Revoke(now));
            return Task.FromResult<IReadOnlyList<Guid>>([.. active.Select(d => d.Id)]);
        }
    }
}
