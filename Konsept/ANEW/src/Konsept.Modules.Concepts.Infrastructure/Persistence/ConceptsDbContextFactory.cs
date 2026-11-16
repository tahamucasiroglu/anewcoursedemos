using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Konsept.Modules.Concepts.Infrastructure.Persistence;

/// <summary>Yalnızca tasarım zamanı (<c>dotnet ef migrations</c>) için bağlam üretir; veritabanına bağlanmaz.</summary>
public sealed class ConceptsDbContextFactory : IDesignTimeDbContextFactory<ConceptsDbContext>
{
    /// <inheritdoc />
    public ConceptsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ConceptsDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time", o => o.MigrationsHistoryTable("__EFMigrationsHistory", ConceptsDbContext.SchemaName))
            .Options;
        return new ConceptsDbContext(options);
    }
}
