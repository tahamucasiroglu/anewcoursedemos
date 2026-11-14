using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;

namespace TahaKonsept.Modules.Profiles.Infrastructure.Persistence.Concrete;

/// <summary>
/// <c>dotnet ef</c> araçları için <see cref="ProfilesDbContext"/> factory'si.
/// </summary>
public sealed class ProfilesDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactoryBase<ProfilesDbContext>
{
    /// <summary>Modül şeması.</summary>
    protected override string Schema => ProfilesDbContext.SchemaName;

    /// <summary>DbContext oluşturur.</summary>
    /// <param name="options">EF seçenekleri.</param>
    /// <param name="clock">Saat.</param>
    /// <returns>DbContext.</returns>
    protected override ProfilesDbContext CreateContext(DbContextOptions<ProfilesDbContext> options, IClock clock) => new(options, clock);
}
