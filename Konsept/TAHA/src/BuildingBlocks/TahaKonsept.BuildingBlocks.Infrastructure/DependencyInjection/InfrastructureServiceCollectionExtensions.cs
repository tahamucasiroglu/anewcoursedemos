using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Security;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Concrete;
using TahaKonsept.BuildingBlocks.Infrastructure.Security.Concrete;
using TahaKonsept.BuildingBlocks.Infrastructure.Storage.Concrete;
using TahaKonsept.BuildingBlocks.Infrastructure.Time.Concrete;

namespace TahaKonsept.BuildingBlocks.Infrastructure.DependencyInjection;

/// <summary>
/// BuildingBlocks altyapı servislerinin kaydı: <see cref="IClock"/> (SystemClock), <see cref="IAppDataLocation"/>,
/// <see cref="JwtOptions"/> (tekil örnek, <c>Jwt</c> bölümü), <see cref="IJwtSigningKeyProvider"/>, <see cref="ISecretProtector"/> (AES-GCM),
/// <see cref="IDatabaseStatus"/> (tekil <see cref="DatabaseStatus"/>).
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Altyapı servislerini ekler.</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="configuration">Yapılandırma.</param>
    /// <param name="contentRootPath">İçerik kökü (App_Data çözümü için).</param>
    /// <returns>Servisler.</returns>
    public static IServiceCollection AddBuildingBlocksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var appData = AppDataLocation.Resolve(configuration, contentRootPath);
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddSingleton<IAppDataLocation>(appData);
        services.TryAddSingleton(jwtOptions);
        services.TryAddSingleton<IJwtSigningKeyProvider, JwtSigningKeyProvider>();
        services.TryAddSingleton<ISecretProtector, AesGcmSecretProtector>();
        services.TryAddSingleton<DatabaseStatus>();
        services.TryAddSingleton<IDatabaseStatus>(sp => sp.GetRequiredService<DatabaseStatus>());
        return services;
    }
}
