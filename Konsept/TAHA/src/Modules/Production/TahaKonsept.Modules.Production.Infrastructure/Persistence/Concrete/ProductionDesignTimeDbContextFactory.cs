using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>
/// <c>dotnet ef</c> araçları için <see cref="ProductionDbContext"/> factory'si.
/// </summary>
public sealed class ProductionDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactoryBase<ProductionDbContext>
{
    /// <summary>Modül şeması.</summary>
    protected override string Schema => ProductionDbContext.SchemaName;

    /// <summary>DbContext oluşturur.</summary>
    /// <param name="options">EF seçenekleri.</param>
    /// <param name="clock">Saat.</param>
    /// <returns>DbContext.</returns>
    protected override ProductionDbContext CreateContext(DbContextOptions<ProductionDbContext> options, IClock clock) => new(options, clock);
}
