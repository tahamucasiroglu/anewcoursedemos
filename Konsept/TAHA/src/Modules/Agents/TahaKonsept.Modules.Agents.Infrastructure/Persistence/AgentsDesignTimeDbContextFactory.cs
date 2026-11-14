using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;

namespace TahaKonsept.Modules.Agents.Infrastructure.Persistence;

/// <summary>
/// <c>dotnet ef</c> için <see cref="AgentsDbContext"/> üreticisi (bağlantı: <c>TAHAKONSEPT_DB</c> ya da varsayılan geliştirme bağlantısı).
/// </summary>
public sealed class AgentsDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactoryBase<AgentsDbContext>
{
    /// <summary>Şema.</summary>
    protected override string Schema => AgentsDbContext.SchemaName;

    /// <summary>DbContext oluşturur.</summary>
    /// <param name="options">Seçenekler.</param>
    /// <param name="clock">Saat.</param>
    /// <returns>DbContext.</returns>
    protected override AgentsDbContext CreateContext(DbContextOptions<AgentsDbContext> options, IClock clock) => new(options, clock);
}
