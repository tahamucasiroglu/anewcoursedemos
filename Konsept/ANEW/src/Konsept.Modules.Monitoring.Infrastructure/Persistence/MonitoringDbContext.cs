using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence;

/// <summary>
/// Monitoring modülünün veritabanı bağlamı (<c>monitoring</c> şeması).
/// </summary>
public sealed class MonitoringDbContext : ModuleDbContextBase
{
    /// <summary>Şema adı.</summary>
    public const string SchemaName = "monitoring";

    /// <summary>Initializes a new instance of the <see cref="MonitoringDbContext"/> class.</summary>
    /// <param name="options">EF Core seçenekleri.</param>
    public MonitoringDbContext(DbContextOptions<MonitoringDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets genel olay kaydı.</summary>
    public DbSet<SystemEvent> SystemEvents => Set<SystemEvent>();

    /// <summary>Gets veritabanındaki teknik log (spec 0008).</summary>
    public DbSet<TechnicalLog> TechnicalLogs => Set<TechnicalLog>();

    /// <summary>Gets hata bildirimleri (spec 0022).</summary>
    public DbSet<ErrorReport> ErrorReports => Set<ErrorReport>();

    /// <inheritdoc />
    protected override string Schema => SchemaName;
}
