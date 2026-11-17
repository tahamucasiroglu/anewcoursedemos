using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Dtos;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence.Concrete;

/// <summary>Hata bildirimlerinin EF Core uygulaması (<c>monitoring.error_reports</c>; numara identity sütunundan).</summary>
public sealed class ErrorReportRepository : EfRepositoryBase<MonitoringDbContext>, IErrorReportRepository
{
    /// <summary>Initializes a new instance of the <see cref="ErrorReportRepository"/> class.</summary>
    /// <param name="context">Monitoring veritabanı bağlamı.</param>
    public ErrorReportRepository(MonitoringDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task AddAsync(ErrorReport report, CancellationToken cancellationToken)
    {
        Context.ErrorReports.Add(report);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<int> CountUserReportsSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken) =>
        Context.ErrorReports
            .AsNoTracking()
            .CountAsync(r => r.UserId == userId && r.Source != ErrorReportSource.Api && r.CreatedAt >= since, cancellationToken);

    /// <inheritdoc />
    public Task<ErrorReport?> GetByNumberAsync(long number, CancellationToken cancellationToken) =>
        Context.ErrorReports.AsNoTracking().FirstOrDefaultAsync(r => r.Number == number, cancellationToken);

    /// <inheritdoc />
    public async Task<ErrorReportPage> ListAsync(
        ErrorReportSource? source,
        IReadOnlyCollection<ErrorReportStatus>? statuses,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = Context.ErrorReports.AsNoTracking();
        if (source is { } only)
        {
            query = query.Where(r => r.Source == only);
        }

        if (statuses is not null)
        {
            var set = statuses.ToList();
            query = query.Where(r => set.Contains(r.Status));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query.OrderByDescending(r => r.Number).Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
        return new ErrorReportPage(items, total);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateStatusAsync(long number, ErrorReportStatus status, string? note, DateTimeOffset at, CancellationToken cancellationToken) =>
        await Context.ErrorReports
            .Where(r => r.Number == number)
            .ExecuteUpdateAsync(
                s => s.SetProperty(r => r.Status, status).SetProperty(r => r.AdminNote, note).SetProperty(r => r.StatusChangedAt, at.ToUniversalTime()),
                cancellationToken)
            .ConfigureAwait(false) == 1;
}
