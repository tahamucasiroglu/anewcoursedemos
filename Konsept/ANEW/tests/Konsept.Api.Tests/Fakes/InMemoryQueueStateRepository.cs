using System.Collections.Concurrent;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Sıra durumunun bellek deposu (plan 0017).</summary>
public sealed class InMemoryQueueStateRepository : IQueueStateRepository
{
    private readonly ConcurrentDictionary<Guid, QueueState> _rows = new();

    /// <inheritdoc />
    public Task<QueueState?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_rows.TryGetValue(userId, out var q) ? q : null);

    /// <inheritdoc />
    public Task SetAsync(QueueState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        _rows[state.UserId] = state;
        return Task.CompletedTask;
    }
}
