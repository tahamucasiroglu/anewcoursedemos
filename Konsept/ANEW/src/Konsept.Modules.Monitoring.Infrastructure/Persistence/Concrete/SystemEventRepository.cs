using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence.Concrete;

/// <summary>
/// Olay kaydının EF Core uygulaması.
/// </summary>
public sealed class SystemEventRepository : EfRepositoryBase<MonitoringDbContext>, ISystemEventRepository
{
    /// <summary>Initializes a new instance of the <see cref="SystemEventRepository"/> class.</summary>
    /// <param name="context">Monitoring veritabanı bağlamı.</param>
    public SystemEventRepository(MonitoringDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task AddAsync(SystemEvent systemEvent, CancellationToken cancellationToken)
    {
        Context.SystemEvents.Add(systemEvent);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<SystemEvent?> GetLatestAsync(CancellationToken cancellationToken) =>
        Context.SystemEvents
            .AsNoTracking()
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public Task<SystemEvent?> GetLatestVisibleAsync(Guid? userId, CancellationToken cancellationToken) =>
        Context.SystemEvents
            .AsNoTracking()
            .Where(e => e.UserId == null || (userId != null && e.UserId == userId))
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public Task<SystemEvent?> GetLatestOwnedAsync(Guid userId, CancellationToken cancellationToken) =>
        Context.SystemEvents
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemEvent>> ListOwnedAsync(
        Guid userId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        IReadOnlyList<EventLevel>? levels,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = Context.SystemEvents
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.OccurredAt >= fromUtc && e.OccurredAt < toUtc);
        if (levels is not null)
        {
            query = query.Where(e => levels.Contains(e.Level));
        }

        return await query
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<DateTimeOffset?> GetEarliestOwnedAsync(Guid userId, CancellationToken cancellationToken) =>
        Context.SystemEvents
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .MinAsync(e => (DateTimeOffset?)e.OccurredAt, cancellationToken);
}
