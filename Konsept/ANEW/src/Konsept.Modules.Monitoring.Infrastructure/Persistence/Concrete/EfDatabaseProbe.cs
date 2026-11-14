using Konsept.Modules.Monitoring.Infrastructure.Persistence.Base;

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence.Concrete;

/// <summary>
/// Monitoring bağlamı üzerinden PostgreSQL'e bağlanmayı dener.
/// </summary>
public sealed class EfDatabaseProbe : DatabaseProbeBase
{
    private readonly MonitoringDbContext _context;

    /// <summary>Initializes a new instance of the <see cref="EfDatabaseProbe"/> class.</summary>
    /// <param name="context">Monitoring veritabanı bağlamı.</param>
    public EfDatabaseProbe(MonitoringDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    protected override Task<bool> ProbeAsync(CancellationToken cancellationToken) =>
        _context.Database.CanConnectAsync(cancellationToken);
}
