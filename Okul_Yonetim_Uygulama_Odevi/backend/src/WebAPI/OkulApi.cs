using NArchitecture.Core.Localization.Resource.Yaml.DependencyInjection;
using OkulYonetim.Application;
using OkulYonetim.Infrastructure;
using OkulYonetim.Persistence;
using OkulYonetim.WebAPI.Auth;
using OkulYonetim.WebAPI.Cors;
using OkulYonetim.WebAPI.Errors;
using OkulYonetim.WebAPI.Logging;
using Serilog;
using Serilog.Events;

namespace OkulYonetim.WebAPI;

/// <summary>
/// API'nin kurulumu (servisler + HTTP hattı). Sıra: izleme numarası → istek logu → hata yakalama →
/// CORS → kimlik doğrulama (JWT + oturum kontrolü) → yetki (varsayılan reddet) → giriş hız sınırı → controller'lar. İstek satırı Information'dır; hata, hata middleware'inde bir kez Error olarak yazılır.
/// Ayarlar ortamdan gelir (adres, bağlantı, CORS, log klasörü — ADR-0004).
/// "Database:MigrateOnStartup" açıksa başlangıçta şema geçişleri uygulanır; veritabanı yoksa API yine açılır.
/// </summary>
public static class OkulApi
{
    public static WebApplication Build(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Yanıtlarda sunucu bilgisi sızdırılmaz ("Server: Kestrel" başlığı kapalı) — spec 0001, gereksinim 1.
        builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

        builder.Services.AddSerilog(
            (services, configuration) => LoggingSetup.Configure(configuration, builder.Configuration).ReadFrom.Services(services),
            preserveStaticLogger: true);

        builder.Services.AddApplicationServices();
        builder.Services.AddPersistenceServices(builder.Configuration);
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddOkulAuth(builder.Configuration);
        builder.Services.AddLoginRateLimit(builder.Configuration);
        builder.Services.AddYamlResourceLocalization();
        builder.Services.AddOkulCors(builder.Configuration);
        builder.Services.AddControllers();
        builder.Services.AddOpenApi(options => options.AddSchemaTransformer((schema, _, _) =>
        {
            // XML açıklamaları Windows'ta CRLF ile gelir; üretilen openapi.json her makinede aynı olsun (LF).
            if (schema.Description is not null)
            {
                schema.Description = schema.Description.Replace("\r\n", "\n", StringComparison.Ordinal);
            }

            return Task.CompletedTask;
        }));

        WebApplication app = builder.Build();

        app.UseForwardedHeaders();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.Logger = app.Services.GetRequiredService<Serilog.ILogger>();
            options.GetLevel = (_, _, _) => LogEventLevel.Information;
        });
        app.UseMiddleware<TraceIdExceptionMiddleware>();
        app.UseRouting();
        app.UseCors(CorsSetup.PolicyName);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapControllers();
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().AllowAnonymous();
        }

        if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            MigrateDatabase(app);
        }

        return app;
    }

    private static void MigrateDatabase(WebApplication app)
    {
        try
        {
            app.Services.MigrateOkulDatabase();
        }
#pragma warning disable CA1031 // Veritabanı yoksa API yine açılır; sistem durumu "hizmet dışı" gösterir. Hata loglanır.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            app.Logger.LogCritical(exception, "Başlangıçta veritabanı şema geçişi uygulanamadı");
        }
    }
}
