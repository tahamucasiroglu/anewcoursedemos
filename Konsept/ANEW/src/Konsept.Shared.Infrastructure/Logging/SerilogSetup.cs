using Konsept.Shared.Logging;
using Konsept.Shared.Logging.Concrete;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Konsept.Shared.Infrastructure.Logging;

/// <summary>
/// Api'nin Serilog kurulumu (spec 0008): ortak zenginleştirme ve maskeleme, konsol + günlük dosya, 7 gün saklama,
/// istek başına korelasyon numarası. Klasör yalnızca <c>Logging:Directory</c> ayarından ya da uygulama klasörü
/// altındaki <c>logs</c>'tan gelir.
/// </summary>
public static class SerilogSetup
{
    /// <summary>Uygulamaya Serilog'u ekler.</summary>
    /// <param name="builder">Web uygulaması kurucusu.</param>
    /// <param name="application">Kaynak uygulama (<see cref="LogSources"/>).</param>
    /// <param name="filePrefix">Log dosyası öneki (ör. <c>api</c>).</param>
    /// <param name="configureSinks">Ek sink'ler (ör. veritabanı) — servis sağlayıcıya erişir.</param>
    public static void AddKonseptSerilog(
        this WebApplicationBuilder builder,
        string application,
        string filePrefix,
        Action<LoggerConfiguration, IServiceProvider>? configureSinks = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        var directory = LogDirectory(builder.Configuration);
        builder.Services.AddSerilog((services, logger) =>
        {
            logger
                .ReadFrom.Configuration(builder.Configuration)
                .WithKonseptEnrichment(application)
                .WriteTo.Console(KonseptLogging.CreateFormatter())
                .WriteToKonseptFile(directory, filePrefix);
            configureSinks?.Invoke(logger, services);
        },
        preserveStaticLogger: true); // Logger bu host'a ait; statik Log.Logger'a bağlanmaz (testlerde paralel host'lar).
        builder.Services.AddSingleton(services =>
            new LogFileCleanupScheduler(new LogFileJanitor(services.GetRequiredService<TimeProvider>()), directory, services.GetRequiredService<TimeProvider>()));
    }

    /// <summary>Korelasyon numarasını loglara ekler ve log klasörü temizliğini başlatır.</summary>
    /// <param name="app">Uygulama.</param>
    public static void UseKonseptLogging(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMiddleware<CorrelationLogMiddleware>();
        app.UseSerilogRequestLogging();
        app.Services.GetRequiredService<LogFileCleanupScheduler>().Start();
    }

    /// <summary>Log klasörü: <c>Logging:Directory</c> ya da uygulama klasörü altındaki <c>logs</c>.</summary>
    /// <param name="configuration">Yapılandırma.</param>
    /// <returns>Klasör yolu.</returns>
    public static string LogDirectory(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration["Logging:Directory"] ?? Path.Combine(AppContext.BaseDirectory, "logs");
    }
}
