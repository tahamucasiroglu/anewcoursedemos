using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;

namespace TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence;

/// <summary>
/// Diagnostics modülünün DbContext'i (şema <c>diagnostics</c>): <see cref="ActivityLogs"/> ve <see cref="ErrorReports"/>.
/// Yapılandırmalar aynı derlemedeki <c>IEntityTypeConfiguration</c>'lardan otomatik uygulanır.
/// </summary>
public sealed class DiagnosticsDbContext : ModuleDbContextBase
{
    /// <summary>Şema adı.</summary>
    public const string SchemaName = "diagnostics";

    /// <summary>Oluşturur.</summary>
    /// <param name="options">EF seçenekleri.</param>
    /// <param name="clock">Saat.</param>
    public DiagnosticsDbContext(DbContextOptions<DiagnosticsDbContext> options, IClock clock)
        : base(options, clock)
    {
    }

    /// <summary>Şema.</summary>
    public override string Schema => SchemaName;

    /// <summary>İşlem günlüğü kayıtları.</summary>
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    /// <summary>Hata bildirimleri.</summary>
    public DbSet<ErrorReport> ErrorReports => Set<ErrorReport>();
}
