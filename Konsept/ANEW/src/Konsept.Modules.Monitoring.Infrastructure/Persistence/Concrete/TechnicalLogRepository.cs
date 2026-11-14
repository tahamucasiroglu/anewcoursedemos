using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence.Concrete;

/// <summary>Teknik log kaydının EF Core uygulaması.</summary>
/// <param name="context">Monitoring veritabanı bağlamı.</param>
public sealed class TechnicalLogRepository(MonitoringDbContext context)
    : EfRepositoryBase<MonitoringDbContext>(context), ITechnicalLogRepository
{
    /// <inheritdoc />
    public async Task AddRangeAsync(IReadOnlyList<TechnicalLog> logs, CancellationToken cancellationToken)
    {
        Context.TechnicalLogs.AddRange(logs);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken) =>
        Context.TechnicalLogs.Where(l => l.OccurredAt < cutoff).ExecuteDeleteAsync(cancellationToken);
}
