using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;

namespace TahaKonsept.Modules.Providers.Infrastructure.Persistence;

/// <summary>
/// <c>dotnet ef</c> için tasarım zamanı fabrikası (bağlantı: <c>TAHAKONSEPT_DB</c> ya da varsayılan geliştirme bağlantısı).
/// </summary>
public sealed class ProvidersDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactoryBase<ProvidersDbContext>
{
    /// <inheritdoc />
    protected override string Schema => ProvidersDbContext.SchemaName;

    /// <inheritdoc />
    protected override ProvidersDbContext CreateContext(DbContextOptions<ProvidersDbContext> options, IClock clock) =>
        new(options, clock);
}
