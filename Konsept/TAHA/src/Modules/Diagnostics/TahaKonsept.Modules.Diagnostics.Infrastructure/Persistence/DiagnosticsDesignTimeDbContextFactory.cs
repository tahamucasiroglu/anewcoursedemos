using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;

namespace TahaKonsept.Modules.Diagnostics.Infrastructure.Persistence;

/// <summary>
/// <c>dotnet ef migrations add ... -p/-s TahaKonsept.Modules.Diagnostics.Infrastructure</c> için design-time factory.
/// </summary>
public sealed class DiagnosticsDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactoryBase<DiagnosticsDbContext>
{
    /// <summary>Şema.</summary>
    protected override string Schema => DiagnosticsDbContext.SchemaName;

    /// <summary>DbContext oluşturur.</summary>
    /// <param name="options">EF seçenekleri.</param>
    /// <param name="clock">Saat.</param>
    /// <returns>DbContext.</returns>
    protected override DiagnosticsDbContext CreateContext(DbContextOptions<DiagnosticsDbContext> options, IClock clock) => new(options, clock);
}
