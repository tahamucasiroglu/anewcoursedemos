using System.Collections.Concurrent;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;

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

    /// <inheritdoc />
    public Task<SystemEvent?> GetLatestVisibleAsync(Guid? userId, CancellationToken cancellationToken) =>
        Task.FromResult(_events
            .Where(e => e.UserId is null || (userId is not null && e.UserId == userId))
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .FirstOrDefault());

    /// <inheritdoc />
    public Task<SystemEvent?> GetLatestOwnedAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_events
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .FirstOrDefault());

    /// <inheritdoc />
    public Task<IReadOnlyList<SystemEvent>> ListOwnedAsync(
        Guid userId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        IReadOnlyList<EventLevel>? levels,
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SystemEvent>>(_events
            .Where(e => e.UserId == userId && e.OccurredAt >= fromUtc && e.OccurredAt < toUtc && (levels is null || levels.Contains(e.Level)))
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .Skip(skip)
            .Take(take)
            .ToList());

    /// <inheritdoc />
    public Task<DateTimeOffset?> GetEarliestOwnedAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_events.Where(e => e.UserId == userId).Select(e => (DateTimeOffset?)e.OccurredAt).Min());
}
