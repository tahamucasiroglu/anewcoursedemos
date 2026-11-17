using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Application.Concrete;
using Konsept.Modules.Production.Application.Dtos;
using Konsept.Modules.Production.Infrastructure.Hosting;
using Konsept.Modules.Production.Infrastructure.Persistence;
using Konsept.Modules.Production.Infrastructure.Persistence.Concrete;
using Konsept.Modules.Production.Infrastructure.Pipeline.Concrete;
using Konsept.Shared.Infrastructure.Http.Concrete;
using Konsept.Shared.Kernel.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Konsept.Modules.Production.Infrastructure;

/// <summary>Production modülünün servis kaydı ve açılış migration'ı (spec 0006, 0017: üretim hattı, koşucu, zamanlayıcı).</summary>
public static class ProductionModule
{
    /// <summary>Modülün servislerini kaydeder.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="connectionString">PostgreSQL bağlantı metni.</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddProductionModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ProductionDbContext>(o => o.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ProductionDbContext.SchemaName)));
        services.AddScoped<IProductionJobRepository, ProductionJobRepository>();
        services.AddScoped<IProductionSettingsRepository, ProductionSettingsRepository>();
        services.AddScoped<IStageOutputRepository, StageOutputRepository>();
        services.AddScoped<IProducedVideoRepository, ProducedVideoRepository>();
        services.AddScoped<IClipStatistics, ClipStatistics>(); // Monitoring.Contracts: admin klip sayısı (spec 0010).
        services.AddScoped<IQueueStateRepository, QueueStateRepository>();
        services.AddScoped<ProductionPipelineLinks>();
        services.AddScoped<IProductionService, ProductionService>();
        services.AddScoped<IProducedVideoService, ProducedVideoService>();
        services.AddScoped<IVideoVersionService, VideoVersionService>(); // spec 0020.
        services.AddScoped<IDislikedVideoTitles, DislikedVideoTitles>(); // Concepts.Contracts: "beğenilmedi" negatif sinyali (spec 0019).
        services.AddScoped<IProductionPipeline, ProductionPipeline>();
        services.AddScoped<IProductionActivityReader, ProductionActivityReader>(); // AgentGateway.Contracts: iş sürerken güncelleme yok (spec 0023).
        services.AddSingleton<IProductionRunner, ProductionRunner>();
        services.AddHostedService<ProductionWorker>();
        services.AddScoped<IUserSessionsEndedListener, ProductionSessionListener>(); // Identity.Contracts: çıkışta üretim durur (Req 16).
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpContextCurrentUser>();
        return services;
    }

    /// <summary>Modülün şemasını bekleyen migration'larla günceller.</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public static async Task MigrateProductionDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ProductionDbContext>();
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
