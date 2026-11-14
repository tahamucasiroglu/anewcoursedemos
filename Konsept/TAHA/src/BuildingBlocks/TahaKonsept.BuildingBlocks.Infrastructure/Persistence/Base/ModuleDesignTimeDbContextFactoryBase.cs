using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Infrastructure.Time.Concrete;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;

/// <summary>
/// <c>dotnet ef migrations add</c> için design-time factory tabanı. Bağlantı: <c>TAHAKONSEPT_DB</c> ya da
/// <c>Host=localhost;Port=5432;Database=tahakonsept;Username=tahakonsept;Password=dev</c>.
/// Türetilen sınıf yalnızca <see cref="Schema"/> ve <see cref="CreateContext"/> yazar.
/// </summary>
/// <typeparam name="TContext">Modül DbContext türü.</typeparam>
public abstract class ModuleDesignTimeDbContextFactoryBase<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : ModuleDbContextBase
{
    /// <summary>Modül şeması.</summary>
    protected abstract string Schema { get; }

    /// <summary>DbContext oluşturur (EF araçları çağırır).</summary>
    /// <param name="args">Araç argümanları.</param>
    /// <returns>DbContext.</returns>
    public TContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TContext>();
        ModuleDbContextOptionsConfigurator.Configure(builder, DatabaseConnectionString.ResolveDesignTime(), Schema, typeof(TContext).Assembly);
        return CreateContext(builder.Options, new SystemClock());
    }

    /// <summary>Somut DbContext'i oluşturur.</summary>
    /// <param name="options">EF seçenekleri.</param>
    /// <param name="clock">Saat.</param>
    /// <returns>DbContext.</returns>
    protected abstract TContext CreateContext(DbContextOptions<TContext> options, IClock clock);
}
