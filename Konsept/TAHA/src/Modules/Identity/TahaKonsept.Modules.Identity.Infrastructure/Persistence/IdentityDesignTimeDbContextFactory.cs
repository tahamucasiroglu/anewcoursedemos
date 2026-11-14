using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence;

namespace TahaKonsept.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// <c>dotnet ef migrations add</c> için design-time factory. <see cref="IdentityModuleDbContext"/> <c>ModuleDbContextBase</c>'ten
/// türemediği için ortak taban yerine doğrudan <see cref="IDesignTimeDbContextFactory{TContext}"/> uygular; ayarlar aynıdır
/// (<see cref="ModuleDbContextOptionsConfigurator"/>, bağlantı <see cref="DatabaseConnectionString.ResolveDesignTime"/>).
/// </summary>
public sealed class IdentityDesignTimeDbContextFactory : IDesignTimeDbContextFactory<IdentityModuleDbContext>
{
    /// <summary>DbContext oluşturur (EF araçları çağırır).</summary>
    /// <param name="args">Araç argümanları.</param>
    /// <returns>DbContext.</returns>
    public IdentityModuleDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<IdentityModuleDbContext>();
        ModuleDbContextOptionsConfigurator.Configure(
            builder,
            DatabaseConnectionString.ResolveDesignTime(),
            IdentityModuleDbContext.SchemaName,
            typeof(IdentityModuleDbContext).Assembly);
        return new IdentityModuleDbContext(builder.Options);
    }
}
