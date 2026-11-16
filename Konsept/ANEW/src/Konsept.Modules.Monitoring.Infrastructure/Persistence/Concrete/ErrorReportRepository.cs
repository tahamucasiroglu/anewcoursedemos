using Konsept.Modules.Monitoring.Application.Abstract;
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
}
