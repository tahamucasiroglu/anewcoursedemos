using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TahaKonsept.BuildingBlocks.Api.Modules.Base;
using TahaKonsept.Modules.Production.Infrastructure.DependencyInjection;
using TahaKonsept.Modules.Production.Infrastructure.Persistence.Concrete;

namespace TahaKonsept.Modules.Production.Api;

/// <summary>
/// Production modülü kaydı: <see cref="ProductionDbContext"/> (şema <c>production</c>), servisler, aşama işleyicileri, orkestratör BackgroundService'i,
/// ajan olay işleyicisi (<c>IAgentStageEventHandler</c> — <c>Replace</c> ile; liste sırasından bağımsız) ve migration'lar.
/// Rotalar: <c>api/production/settings</c>, <c>api/production/queue...</c>, <c>api/production/videos...</c>, <c>api/production/pronunciations...</c>,
/// <c>api/production/jobs/{id}</c>.
/// </summary>
public sealed class ProductionModule : ModuleBase
{
    /// <summary>Modül adı.</summary>
    public override string Name => "Production";

    /// <summary>Servisleri kaydeder.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    public override void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddProductionModule(configuration);

    /// <summary>Migration'ları uygular.</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        MigrateDbContextAsync<ProductionDbContext>(services, cancellationToken);
}
