using System.Collections.Concurrent;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Hızlı profil testleri için bellekte olay deposu (gerçek PostgreSQL entegrasyon testlerinde kullanılmaz).
/// </summary>
public sealed class InMemorySystemEventRepository : ISystemEventRepository
{
    private readonly ConcurrentQueue<SystemEvent> _events = new();

    /// <summary>Gets kaydedilen olaylar.</summary>
    public IReadOnlyCollection<SystemEvent> Events => _events;

    /// <inheritdoc />
    public Task AddAsync(SystemEvent systemEvent, CancellationToken cancellationToken)
    {
        _events.Enqueue(systemEvent);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<SystemEvent?> GetLatestAsync(CancellationToken cancellationToken) =>
        Task.FromResult(_events.OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id).FirstOrDefault());
}
