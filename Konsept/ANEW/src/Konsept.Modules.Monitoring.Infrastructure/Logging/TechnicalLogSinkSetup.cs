using Konsept.Modules.Monitoring.Contracts.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Konsept.Modules.Monitoring.Infrastructure.Logging;

/// <summary>Veritabanı teknik log sink'inin Serilog'a eklenmesi (spec 0008 Req 6).</summary>
public static partial class TechnicalLogSinkSetup
{
    /// <summary>Warning+ logları toplu olarak veritabanındaki teknik loga yazan sink'i ekler.</summary>
    /// <param name="configuration">Logger yapılandırması.</param>
    /// <param name="services">Kök servis sağlayıcı.</param>
    /// <param name="source">Kaynak uygulama.</param>
    /// <returns>Aynı yapılandırma.</returns>
    public static LoggerConfiguration WriteToTechnicalLogDatabase(this LoggerConfiguration configuration, IServiceProvider services, LogEntrySource source)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(services);

        // Logger fabrikası bu kurulumun kendisine bağlı; döngü olmasın diye ancak hata anında çözülür.
        var sink = new DatabaseLogSink(
            services,
            source,
            ex => LogWriteFailed(services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseLogSink).FullName!), ex),
            services.GetRequiredService<TimeProvider>());
        return configuration.WriteTo.Sink(
            sink,
            new BatchingOptions { BatchSizeLimit = 100, BufferingTimeLimit = TimeSpan.FromSeconds(2), QueueLimit = 10_000 },
            LogEventLevel.Warning);
    }

    [LoggerMessage(EventId = 1103, Level = LogLevel.Warning, Message = "Teknik log veritabanına yazılamadı; kayıtlar yalnızca dosyada")]
    private static partial void LogWriteFailed(Microsoft.Extensions.Logging.ILogger logger, Exception exception);
}
