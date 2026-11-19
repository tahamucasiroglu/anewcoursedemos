using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Localization.Resource.Yaml.DependencyInjection;
using OkulYonetim.Application;
using OkulYonetim.Infrastructure;
using OkulYonetim.Persistence;
using OkulYonetim.Persistence.Contexts;
using OkulYonetim.WebAPI.Cors;
using OkulYonetim.WebAPI.Errors;
using OkulYonetim.WebAPI.Logging;
using Serilog;
using Serilog.Events;

namespace OkulYonetim.WebAPI;

/// <summary>
/// API'nin kurulumu (servisler + HTTP hattı). Sıra: izleme numarası → istek logu → hata yakalama →
/// CORS → controller'lar. İstek satırı Information'dır; hata, hata middleware'inde bir kez Error olarak yazılır.
/// Ayarlar ortamdan gelir (adres, bağlantı, CORS, log klasörü — ADR-0004).
/// "Database:MigrateOnStartup" açıksa başlangıçta şema geçişleri uygulanır; veritabanı yoksa API yine açılır.
/// </summary>
public static class OkulApi
{
    public static WebApplication Build(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSerilog(
            (services, configuration) => LoggingSetup.Configure(configuration, builder.Configuration).ReadFrom.Services(services),
            preserveStaticLogger: true);

        builder.Services.AddApplicationServices();
        builder.Services.AddPersistenceServices(builder.Configuration);
        builder.Services.AddInfrastructureServices();
        builder.Services.AddYamlResourceLocalization();
        builder.Services.AddOkulCors(builder.Configuration);
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        WebApplication app = builder.Build();

        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.Logger = app.Services.GetRequiredService<Serilog.ILogger>();
            options.GetLevel = (_, _, _) => LogEventLevel.Information;
        });
        app.UseMiddleware<TraceIdExceptionMiddleware>();
        app.UseCors(CorsSetup.PolicyName);
        app.MapControllers();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            MigrateDatabase(app);
        }

        return app;
    }

    private static void MigrateDatabase(WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        try
        {
            scope.ServiceProvider.GetRequiredService<OkulDbContext>().Database.Migrate();
        }
#pragma warning disable CA1031 // Veritabanı yoksa API yine açılır; sistem durumu "hizmet dışı" gösterir. Hata loglanır.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            app.Logger.LogCritical(exception, "Başlangıçta veritabanı şema geçişi uygulanamadı");
        }
    }
}
