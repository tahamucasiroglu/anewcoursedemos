using FluentValidation;
using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Concrete;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Application.Validators;
using Konsept.Modules.AiGateway.Infrastructure.Catalog;
using Konsept.Modules.AiGateway.Infrastructure.Persistence;
using Konsept.Modules.AiGateway.Infrastructure.Persistence.Concrete;
using Konsept.Modules.AiGateway.Infrastructure.Security;
using Konsept.Modules.AiGateway.Infrastructure.Validation;
using Konsept.Shared.Infrastructure.Http.Concrete;
using Konsept.Shared.Kernel.Abstract;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Konsept.Modules.AiGateway.Infrastructure;

/// <summary>
/// AiGateway modülünün servis kaydı (katalog, şifreli API key'ler, sağlayıcı doğrulaması) ve açılış migration'ı. Data Protection
/// anahtarları <c>DataProtection:KeysDirectory</c> ayarlıysa orada kalıcıdır (Pi: <c>/opt/anewconcept/keys/api</c>; yedeklenmeli).
/// </summary>
public static class AiGatewayModule
{
    /// <summary>Data Protection anahtar klasörü ayarı.</summary>
    public const string KeysDirectoryConfigurationKey = "DataProtection:KeysDirectory";

    /// <summary>Modülün servislerini kaydeder.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="connectionString">PostgreSQL bağlantı metni.</param>
    /// <param name="configuration">Yapılandırma.</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddAiGatewayModule(this IServiceCollection services, string connectionString, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddDbContext<AiGatewayDbContext>(o => o.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AiGatewayDbContext.SchemaName)));
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddSingleton<ICatalogSource, EmbeddedCatalogSource>();

        var dataProtection = services.AddDataProtection().SetApplicationName("anewconcept-api");
        var keysDirectory = configuration[KeysDirectoryConfigurationKey];
        if (!string.IsNullOrEmpty(keysDirectory))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));
        }

        services.AddSingleton<IApiKeyProtector, DataProtectionApiKeyProtector>();
        // Yönlendirme izlenmez (review r1 L-3): .NET yönlendirmede Authorization'ı siler ama x-api-key'i taşır; 3xx → "Doğrulanmadı".
        services.AddHttpClient(HttpProviderKeyValidator.ClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false })
            .RemoveAllLoggers();
        services.AddSingleton<IProviderKeyValidator, HttpProviderKeyValidator>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddSingleton<IValidator<SaveApiKeyRequest>, SaveApiKeyRequestValidator>();
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();
        return services;
    }

    /// <summary>Modülün şemasını bekleyen migration'larla günceller.</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public static async Task MigrateAiGatewayDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AiGatewayDbContext>();
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
