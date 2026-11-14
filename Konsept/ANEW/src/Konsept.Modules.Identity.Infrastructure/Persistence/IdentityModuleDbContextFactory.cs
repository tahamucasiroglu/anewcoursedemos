using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Konsept.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Yalnızca tasarım zamanı (<c>dotnet ef migrations</c>) için bağlam üretir; veritabanına bağlanmaz.
/// </summary>
public sealed class IdentityModuleDbContextFactory : IDesignTimeDbContextFactory<IdentityModuleDbContext>
{
    /// <inheritdoc />
    public IdentityModuleDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<IdentityModuleDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time", o => o.MigrationsHistoryTable("__EFMigrationsHistory", IdentityModuleDbContext.SchemaName))
            .Options;
        return new IdentityModuleDbContext(options);
    }
}
