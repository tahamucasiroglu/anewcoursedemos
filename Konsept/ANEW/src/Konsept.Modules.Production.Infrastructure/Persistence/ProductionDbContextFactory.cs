using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Konsept.Modules.Production.Infrastructure.Persistence;

/// <summary>Tasarım zamanı (migration üretimi) fabrikası; veritabanına bağlanmaz.</summary>
public sealed class ProductionDbContextFactory : IDesignTimeDbContextFactory<ProductionDbContext>
{
    /// <inheritdoc />
    public ProductionDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ProductionDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time", o => o.MigrationsHistoryTable("__EFMigrationsHistory", ProductionDbContext.SchemaName))
            .Options;
        return new ProductionDbContext(options);
    }
}
