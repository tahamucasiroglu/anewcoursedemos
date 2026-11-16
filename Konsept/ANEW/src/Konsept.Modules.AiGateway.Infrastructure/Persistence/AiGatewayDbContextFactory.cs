using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Konsept.Modules.AiGateway.Infrastructure.Persistence;

/// <summary>Yalnızca tasarım zamanı (<c>dotnet ef migrations</c>) için bağlam üretir; veritabanına bağlanmaz.</summary>
public sealed class AiGatewayDbContextFactory : IDesignTimeDbContextFactory<AiGatewayDbContext>
{
    /// <inheritdoc />
    public AiGatewayDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AiGatewayDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time", o => o.MigrationsHistoryTable("__EFMigrationsHistory", AiGatewayDbContext.SchemaName))
            .Options;
        return new AiGatewayDbContext(options);
    }
}
