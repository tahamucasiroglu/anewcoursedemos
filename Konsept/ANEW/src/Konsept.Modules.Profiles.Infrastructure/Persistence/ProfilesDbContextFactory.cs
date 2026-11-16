using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Konsept.Modules.Profiles.Infrastructure.Persistence;

/// <summary>Yalnızca tasarım zamanı (<c>dotnet ef migrations</c>) için bağlam üretir; veritabanına bağlanmaz.</summary>
public sealed class ProfilesDbContextFactory : IDesignTimeDbContextFactory<ProfilesDbContext>
{
    /// <inheritdoc />
    public ProfilesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ProfilesDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time", o => o.MigrationsHistoryTable("__EFMigrationsHistory", ProfilesDbContext.SchemaName))
            .Options;
        return new ProfilesDbContext(options);
    }
}
