using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;
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
}
