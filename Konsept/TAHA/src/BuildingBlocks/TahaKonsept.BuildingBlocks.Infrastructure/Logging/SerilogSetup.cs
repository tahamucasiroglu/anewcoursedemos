using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Logging;

/// <summary>
/// Ortak Serilog kurulumu (API, Web, ajan): konsol + günlük dönen dosya <c>logs/&lt;app&gt;-YYYYMMDD.log</c>,
/// <see cref="RetainedDays"/> (7) gün saklama. Yapılandırmadaki <c>Serilog</c> bölümü varsayılanları ezebilir.
/// Log klasörü: <c>Logging:Directory</c> → <c>TAHAKONSEPT_LOG_DIR</c> → <c>&lt;contentRoot&gt;/logs</c>.
/// Kullanım: <c>builder.Host.UseTahaSerilog("api");</c>
/// </summary>
public static class SerilogSetup
{
    /// <summary>Log dosyası saklama süresi (gün).</summary>
    public const int RetainedDays = 7;

    /// <summary>Log klasörü ortam değişkeni.</summary>
    public const string LogDirectoryEnvironmentVariable = "TAHAKONSEPT_LOG_DIR";

    private const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    /// <summary>Logger yapılandırmasına Taha Konsept varsayılanlarını uygular.</summary>
    /// <param name="configuration">Logger yapılandırması.</param>
    /// <param name="appName">Uygulama adı (dosya öneki, ör. <c>api</c>).</param>
    /// <param name="logDirectory">Log klasörü.</param>
    /// <param name="appConfiguration">Uygulama yapılandırması (opsiyonel <c>Serilog</c> bölümü için).</param>
    /// <returns>Aynı logger yapılandırması.</returns>
    public static LoggerConfiguration ConfigureTahaDefaults(
        this LoggerConfiguration configuration,
        string appName,
        string logDirectory,
        IConfiguration? appConfiguration = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(appName);
        Directory.CreateDirectory(logDirectory);

        configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", appName)
            .WriteTo.Console(outputTemplate: OutputTemplate, formatProvider: CultureInfo.InvariantCulture)
            .WriteTo.File(
                path: Path.Combine(logDirectory, appName + "-.log"),
                outputTemplate: OutputTemplate,
                formatProvider: CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: RetainedDays,
                shared: true,
                encoding: Encoding.UTF8);

        if (appConfiguration is not null && appConfiguration.GetSection("Serilog").Exists())
        {
            configuration.ReadFrom.Configuration(appConfiguration);
        }

        return configuration;
    }

    /// <summary>Log klasörünü çözer.</summary>
    /// <param name="appConfiguration">Yapılandırma.</param>
    /// <param name="contentRootPath">İçerik kökü.</param>
    /// <returns>Mutlak klasör yolu.</returns>
    public static string ResolveLogDirectory(IConfiguration? appConfiguration, string contentRootPath)
    {
        var configured = appConfiguration?["Logging:Directory"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(Path.IsPathRooted(configured) ? configured : Path.Combine(contentRootPath, configured));
        }

        var fromEnv = Environment.GetEnvironmentVariable(LogDirectoryEnvironmentVariable);
        return Path.GetFullPath(string.IsNullOrWhiteSpace(fromEnv) ? Path.Combine(contentRootPath, "logs") : fromEnv);
    }

    /// <summary>Host'a Serilog'u bağlar (ASP.NET Core / Generic Host).</summary>
    /// <param name="hostBuilder">Host oluşturucu (<c>builder.Host</c>).</param>
    /// <param name="appName">Uygulama adı.</param>
    /// <returns>Aynı host oluşturucu.</returns>
    public static IHostBuilder UseTahaSerilog(this IHostBuilder hostBuilder, string appName)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        return hostBuilder.UseSerilog((context, _, logger) => logger.ConfigureTahaDefaults(
            appName,
            ResolveLogDirectory(context.Configuration, context.HostingEnvironment.ContentRootPath),
            context.Configuration));
    }

    /// <summary>Servis koleksiyonuna Serilog'u ekler (Host kullanmayan uygulamalar, ör. Avalonia ajanı).</summary>
    /// <param name="services">Servisler.</param>
    /// <param name="appName">Uygulama adı.</param>
    /// <param name="logDirectory">Log klasörü.</param>
    /// <param name="appConfiguration">Yapılandırma (opsiyonel).</param>
    /// <returns>Servisler.</returns>
    public static IServiceCollection AddTahaSerilog(
        this IServiceCollection services,
        string appName,
        string logDirectory,
        IConfiguration? appConfiguration = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddSerilog(logger => logger.ConfigureTahaDefaults(appName, logDirectory, appConfiguration));
    }

    /// <summary>Uygulama tam kalkmadan önceki hataları yakalamak için başlangıç logger'ı üretir.</summary>
    /// <param name="appName">Uygulama adı.</param>
    /// <param name="logDirectory">Log klasörü.</param>
    /// <returns>Logger.</returns>
    public static Logger CreateBootstrapLogger(string appName, string logDirectory) =>
        new LoggerConfiguration().ConfigureTahaDefaults(appName, logDirectory).CreateLogger();
}
