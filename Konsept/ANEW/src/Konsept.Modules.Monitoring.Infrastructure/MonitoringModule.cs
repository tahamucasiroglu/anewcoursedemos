using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Concrete;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Infrastructure.Logging;
using Konsept.Modules.Monitoring.Infrastructure.Persistence;
using Konsept.Modules.Monitoring.Infrastructure.Persistence.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Konsept.Modules.Monitoring.Infrastructure;

/// <summary>
/// Monitoring modülünün servis kaydı ve açılış migration'ı.
/// </summary>
public static class MonitoringModule
{
    /// <summary>Modülün servislerini kaydeder.</summary>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="connectionString">PostgreSQL bağlantı metni.</param>
    /// <returns>Aynı koleksiyon.</returns>
    public static IServiceCollection AddMonitoringModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MonitoringDbContext>(o => o.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MonitoringDbContext.SchemaName)));
        services.AddScoped<ISystemEventRepository, SystemEventRepository>();
        services.AddScoped<IDatabaseProbe, EfDatabaseProbe>();
        services.AddScoped<ISystemEventService, SystemEventService>();
        services.AddScoped<ISystemEventWriter>(sp => sp.GetRequiredService<ISystemEventService>());
        services.AddScoped<ISystemEventReader>(sp => sp.GetRequiredService<ISystemEventService>());
        services.AddScoped<ISystemStatusService, SystemStatusService>();
        services.AddScoped<ITechnicalLogRepository, TechnicalLogRepository>();
        services.AddScoped<ITechnicalLogService, TechnicalLogService>();
        services.AddScoped<ITechnicalLogWriter>(sp => sp.GetRequiredService<ITechnicalLogService>());
        services.AddHostedService<TechnicalLogRetentionWorker>();
        return services;
    }

    /// <summary>Modülün şemasını bekleyen migration'larla günceller (boş veritabanında oluşturur).</summary>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public static async Task MigrateMonitoringDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
