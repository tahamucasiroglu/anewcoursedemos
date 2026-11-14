using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;

namespace TahaKonsept.Modules.Concepts.Infrastructure.Persistence.Concrete;

/// <summary>
/// <c>dotnet ef</c> araçları için <see cref="ConceptsDbContext"/> factory'si.
/// </summary>
public sealed class ConceptsDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactoryBase<ConceptsDbContext>
{
    /// <summary>Modül şeması.</summary>
    protected override string Schema => ConceptsDbContext.SchemaName;

    /// <summary>DbContext oluşturur.</summary>
    /// <param name="options">EF seçenekleri.</param>
    /// <param name="clock">Saat.</param>
    /// <returns>DbContext.</returns>
    protected override ConceptsDbContext CreateContext(DbContextOptions<ConceptsDbContext> options, IClock clock) => new(options, clock);
}
