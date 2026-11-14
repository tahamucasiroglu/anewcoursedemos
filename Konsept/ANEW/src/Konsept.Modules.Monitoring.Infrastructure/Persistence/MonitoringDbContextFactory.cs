using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence;

/// <summary>
/// Yalnızca tasarım zamanı (<c>dotnet ef migrations</c>) için bağlam üretir; veritabanına bağlanmaz.
/// </summary>
public sealed class MonitoringDbContextFactory : IDesignTimeDbContextFactory<MonitoringDbContext>
{
    /// <inheritdoc />
    public MonitoringDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MonitoringDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time", o => o.MigrationsHistoryTable("__EFMigrationsHistory", MonitoringDbContext.SchemaName))
            .Options;
        return new MonitoringDbContext(options);
    }
}
