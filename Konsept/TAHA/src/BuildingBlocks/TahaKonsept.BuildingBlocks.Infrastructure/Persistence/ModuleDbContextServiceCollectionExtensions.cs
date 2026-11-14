using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Modül DbContext kaydı: <c>services.AddModuleDbContext&lt;ConceptsDbContext&gt;(configuration, "concepts");</c>
/// Bağlantı <see cref="DatabaseConnectionString.Resolve"/> ile çözülür.
/// </summary>
public static class ModuleDbContextServiceCollectionExtensions
{
    /// <summary>Modül DbContext'ini kaydeder.</summary>
    /// <typeparam name="TContext">Modül DbContext türü.</typeparam>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    /// <param name="schema">Modül şeması.</param>
    /// <returns>Servisler.</returns>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string schema)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddDbContext<TContext>(options => ModuleDbContextOptionsConfigurator.Configure(
            options,
            DatabaseConnectionString.Resolve(configuration),
            schema,
            typeof(TContext).Assembly));
        return services;
    }
}
