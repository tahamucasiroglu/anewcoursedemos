using Konsept.Shared.Logging;
using Konsept.Shared.Logging.Concrete;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Konsept.Agent.Infrastructure.Logging;

/// <summary>
/// Ajanın teknik log kurulumu (spec 0008 Req 1, 4, 5, 7): kaynak "ajan", UTC zaman, maskeleme; dosya yalnızca ajan
/// klasörü altındaki <c>logs</c> klasöründe (<c>agent-yyyyMMdd.log</c>), 7 gün saklama; Error/Critical özetleri
/// bağlıyken API'ye.
/// </summary>
public sealed class AgentLogging : IDisposable
{
    private readonly LogFileCleanupScheduler _cleanup;

    private AgentLogging(Logger logger, HubLogSummarySink summarySink, RecentLogBuffer recentLogs, ErrorAlertSink alertSink, LogFileCleanupScheduler cleanup, string directory)
    {
        Logger = logger;
        SummarySink = summarySink;
        RecentLogs = recentLogs;
        AlertSink = alertSink;
        _cleanup = cleanup;
        Directory = directory;
    }

    /// <summary>Gets logger.</summary>
    public Logger Logger { get; }

    /// <summary>Gets özet sink'i (bağlantı oluşturulunca <see cref="HubLogSummarySink.Attach"/> ile bağlanır).</summary>
    public HubLogSummarySink SummarySink { get; }

    /// <summary>Gets son Warning+ satırlar (spec 0022: hata raporuna eklenir).</summary>
    public RecentLogBuffer RecentLogs { get; }

    /// <summary>Gets hata uyarısı sink'i (merkez oluşturulunca <see cref="ErrorAlertSink.Attach"/> ile bağlanır).</summary>
    public ErrorAlertSink AlertSink { get; }

    /// <summary>Gets log klasörü.</summary>
    public string Directory { get; }

    /// <summary>Ajan logunu kurar ve klasör temizliğini başlatır.</summary>
    /// <param name="agentDirectory">Ajanın klasörü (çalıştığı/kurulduğu klasör).</param>
    /// <param name="timeProvider">Saat kaynağı.</param>
    /// <returns>Kurulu loglama.</returns>
    public static AgentLogging Create(string agentDirectory, TimeProvider timeProvider)
    {
        var directory = Path.Combine(agentDirectory, "logs");
        var sink = new HubLogSummarySink();
        var recent = new RecentLogBuffer();
        var alert = new ErrorAlertSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WithKonseptEnrichment(LogSources.Agent)
            .WriteToKonseptFile(directory, "agent")
            .WriteTo.Sink(recent, LogEventLevel.Warning)
            .WriteTo.Sink(alert, LogEventLevel.Error)
            .WriteTo.Sink(sink, new BatchingOptions { BatchSizeLimit = 20, BufferingTimeLimit = TimeSpan.FromSeconds(1), QueueLimit = 1000 }, LogEventLevel.Error)
            .CreateLogger();
        var cleanup = new LogFileCleanupScheduler(new LogFileJanitor(timeProvider), directory, timeProvider).Start();
        return new AgentLogging(logger, sink, recent, alert, cleanup, directory);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cleanup.Dispose();
        Logger.Dispose();
    }
}
