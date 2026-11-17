using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Dtos;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence.Concrete;

/// <summary>Admin panelinin salt okuma sorguları (spec 0010): <c>monitoring.system_events</c> ve <c>monitoring.error_reports</c>.</summary>
public sealed class AdminActivityRepository : EfRepositoryBase<MonitoringDbContext>, IAdminActivityRepository
{
    /// <summary>Initializes a new instance of the <see cref="AdminActivityRepository"/> class.</summary>
    /// <param name="context">Monitoring veritabanı bağlamı.</param>
    public AdminActivityRepository(MonitoringDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public Task<int> CountOpenErrorReportsAsync(CancellationToken cancellationToken) =>
        Context.ErrorReports.AsNoTracking().CountAsync(r => r.Status == ErrorReportStatus.New || r.Status == ErrorReportStatus.Reviewing, cancellationToken); // açık (spec 0025 Req 2)

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserEventTime>> ListUserEventTimesAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        await Context.SystemEvents
            .AsNoTracking()
            .Where(e => e.UserId != null && e.OccurredAt >= fromUtc && e.OccurredAt < toUtc)
            .Select(e => new UserEventTime(e.UserId!.Value, e.OccurredAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<ActivityPage> ListActivitiesAsync(
        IReadOnlyCollection<Guid>? userIds,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = Context.SystemEvents.AsNoTracking().Where(e => e.UserId != null);
        if (userIds is not null)
        {
            var ids = userIds.ToList();
            query = query.Where(e => ids.Contains(e.UserId!.Value));
        }

        if (fromUtc is { } from)
        {
            query = query.Where(e => e.OccurredAt >= from);
        }

        if (toUtc is { } to)
        {
            query = query.Where(e => e.OccurredAt < to);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.Id)
            .Skip(Math.Max(0, skip))
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return new ActivityPage(items, total);
    }
}
