using System.Globalization;
using Serilog;

namespace OkulYonetim.WebAPI.Logging;

/// <summary>
/// Serilog yapılandırması (ADR-0003): seviye "Serilog:MinimumLevel" ayarından (varsayılan Information);
/// günlük dosya "FileLogging:Folder" klasörüne okul-api-YYYYMMDD.log adıyla, 100 MB'ta bölünerek, 30 gün
/// saklanarak yazılır; "FileLogging:Console" açıksa konsola da yazılır. Satırda zaman, seviye, izleme numarası
/// ve kaynak bulunur.
/// </summary>
public static class LoggingSetup
{
    public const string OutputTemplate =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{TraceId}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    public const string FilePrefix = "okul-api-";
    public const int DefaultRetainedDays = 30;
    public const long DefaultFileSizeLimitBytes = 100L * 1024 * 1024;

    public static LoggerConfiguration Configure(LoggerConfiguration configuration, IConfiguration settings)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(settings);

        string folder = settings.GetValue<string>("FileLogging:Folder") ?? "logs";
        int retainedDays = settings.GetValue("FileLogging:RetainedDays", DefaultRetainedDays);
        long sizeLimit = settings.GetValue("FileLogging:FileSizeLimitBytes", DefaultFileSizeLimitBytes);

        configuration
            .MinimumLevel.Information()
            .ReadFrom.Configuration(settings)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: Path.Combine(folder, FilePrefix + ".log"),
                outputTemplate: OutputTemplate,
                formatProvider: CultureInfo.InvariantCulture,
                fileSizeLimitBytes: sizeLimit,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: null,
                retainedFileTimeLimit: TimeSpan.FromDays(retainedDays));

        if (settings.GetValue<bool>("FileLogging:Console"))
        {
            configuration.WriteTo.Console(outputTemplate: OutputTemplate, formatProvider: CultureInfo.InvariantCulture);
        }

        return configuration;
    }
}
