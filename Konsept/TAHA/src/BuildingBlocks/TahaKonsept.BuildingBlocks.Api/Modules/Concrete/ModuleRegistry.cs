using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Api.Modules.Abstract;

namespace TahaKonsept.BuildingBlocks.Api.Modules.Concrete;

/// <summary>
/// <see cref="IModuleRegistry"/> uygulaması. Modüller host'ta açıkça listelenir (yansıma ile tarama yok);
/// her modülün derlemesi MVC application part olarak eklenir, böylece controller'ları keşfedilir.
/// Kendini <see cref="IModuleRegistry"/> olarak DI'a kaydeder.
/// </summary>
public sealed class ModuleRegistry : IModuleRegistry
{
    /// <summary>Modül listesiyle oluşturur; aynı adlı iki modül hata verir.</summary>
    /// <param name="modules">Modüller.</param>
    public ModuleRegistry(IEnumerable<IModule> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);
        Modules = modules.ToList();
        var duplicate = Modules.GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Modül iki kez kaydedilmiş: {duplicate.Key}");
        }
    }

    /// <summary>Kayıtlı modüller.</summary>
    public IReadOnlyList<IModule> Modules { get; }

    /// <summary>Modül servislerini ve controller derlemelerini kaydeder.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    /// <param name="mvcBuilder">MVC oluşturucu.</param>
    public void RegisterServices(IServiceCollection services, IConfiguration configuration, IMvcBuilder mvcBuilder)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(mvcBuilder);

        services.AddSingleton<IModuleRegistry>(this);
        foreach (var module in Modules)
        {
            module.RegisterServices(services, configuration);
            mvcBuilder.AddApplicationPart(module.GetType().Assembly);
        }
    }

    /// <summary>Migration'ları sırayla uygular.</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        var logger = services.GetRequiredService<ILogger<ModuleRegistry>>();
        foreach (var module in Modules)
        {
            logger.LogInformation("Modül migration'ı uygulanıyor: {Module}", module.Name);
            await module.MigrateAsync(services, cancellationToken);
        }
    }

    /// <summary>Modüllerin ek uçlarını eşler.</summary>
    /// <param name="endpoints">Uç oluşturucu.</param>
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        foreach (var module in Modules)
        {
            module.MapEndpoints(endpoints);
        }
    }
}
