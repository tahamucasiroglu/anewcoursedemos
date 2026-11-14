using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TahaKonsept.BuildingBlocks.Api.Modules.Base;
using TahaKonsept.Modules.Providers.Infrastructure.DependencyInjection;
using TahaKonsept.Modules.Providers.Infrastructure.Persistence;

namespace TahaKonsept.Modules.Providers.Api;

/// <summary>
/// Providers modülü kaydı: servisler (<see cref="ProvidersInfrastructureServiceCollectionExtensions.AddProvidersModule"/>) ve
/// <c>providers</c> şeması migration'ı. Host'ta <c>ApiModules.Create()</c> listesine eklenir.
/// </summary>
public sealed class ProvidersModule : ModuleBase
{
    /// <inheritdoc />
    public override string Name => "Providers";

    /// <inheritdoc />
    public override void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
        services.AddProvidersModule(configuration);

    /// <inheritdoc />
    public override Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken = default) =>
        MigrateDbContextAsync<ProvidersDbContext>(services, cancellationToken);
}
