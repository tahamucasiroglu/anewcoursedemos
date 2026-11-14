using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Concrete;
using TahaKonsept.Modules.Providers.Application.Validators;
using TahaKonsept.Modules.Providers.Infrastructure.Catalog.Concrete;
using TahaKonsept.Modules.Providers.Infrastructure.KeyTesting.Concrete;
using TahaKonsept.Modules.Providers.Infrastructure.Persistence;
using TahaKonsept.Modules.Providers.Infrastructure.Persistence.Repositories.Concrete;
using TahaKonsept.Modules.Providers.Infrastructure.Recording.Concrete;
using TahaKonsept.Modules.Providers.Infrastructure.Security.Concrete;
using TahaKonsept.Modules.Providers.Infrastructure.Stats.Concrete;
using TahaKonsept.Modules.Providers.Infrastructure.TextModels.Concrete;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.DependencyInjection;

/// <summary>
/// Providers modülünün DI kaydı. Tekil: katalog, maliyet hesaplayıcı, çağrı kaydedici, anahtar kasası, model fabrikası,
/// <see cref="IProviderGateway"/> (arka plan servislerinden de doğrudan kullanılabilir). Kapsamlı: depolar, anahtar/kullanım/bütçe
/// servisleri, <see cref="IBudgetGuard"/>, doğrulayıcılar, yönetim paneli harcama katkıcısı (<c>ProvidersStatsContributor</c>).
/// </summary>
public static class ProvidersInfrastructureServiceCollectionExtensions
{
    /// <summary>Modül servislerini ekler.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="configuration">Yapılandırma.</param>
    public static IServiceCollection AddProvidersModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<ProvidersDbContext>(configuration, ProvidersDbContext.SchemaName);
        services.AddHttpClient(TextModelFactory.HttpClientName, c => c.Timeout = TimeSpan.FromMinutes(15));
        services.AddHttpClient<IApiKeyTester, HttpApiKeyTester>(c => c.Timeout = TimeSpan.FromSeconds(20));

        services.AddSingleton<IModelCatalog, EmbeddedModelCatalog>();
        services.AddSingleton<ICostCalculator, CostCalculator>();
        services.AddSingleton<ICallRecorder, ScopedCallRecorder>();
        services.AddSingleton<IApiKeyVault, ScopedApiKeyVault>();
        services.AddSingleton<ITextModelFactory, TextModelFactory>();
        services.AddSingleton<IProviderGateway, ProviderGateway>();

        services.AddScoped<IProviderApiKeyRepository, ProviderApiKeyRepository>();
        services.AddScoped<ICallLogRepository, CallLogRepository>();
        services.AddScoped<IModelPriceRepository, ModelPriceRepository>();
        services.AddScoped<IBudgetSettingRepository, BudgetSettingRepository>();
        services.AddScoped<IModelPriceResolver, ModelPriceResolver>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IUsageService, UsageService>();
        services.AddScoped<IBudgetService, BudgetService>();
        services.AddScoped<IBudgetGuard, BudgetGuard>();

        services.AddScoped<IValidator<SaveApiKeyRequest>, SaveApiKeyRequestValidator>();
        services.AddScoped<IValidator<BudgetSettings>, BudgetSettingsValidator>();

        // Yönetim paneli harcama istatistikleri (Diagnostics katkıcısı).
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStatsContributor, ProvidersStatsContributor>());
        return services;
    }
}
