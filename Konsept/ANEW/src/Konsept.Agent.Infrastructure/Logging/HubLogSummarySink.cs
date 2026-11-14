using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Shared.Kernel.Security;
using Serilog.Core;
using Serilog.Events;

namespace Konsept.Agent.Infrastructure.Logging;

/// <summary>
/// Ajanın Error ve Critical loglarını, ajan API'ye bağlıyken maskelenmiş özet olarak (seviye, zaman, mesaj, sürüm)
/// mevcut ajan bağlantısından gönderir (spec 0008 Req 7). Information/Warning ve tam log gönderilmez; bağlı değilken
/// oluşan özetler sonradan da gönderilmez. Bağlantı kendi Warning'lerini yazar, Error yazmaz; bu sink de gönderim
/// hatasını loglamaz — döngü olmaz. Çerçeve adaptörü (Serilog sink'i).
/// </summary>
public sealed class HubLogSummarySink : IBatchedLogEventSink
{
    private IServerConnection? _connection;

    /// <summary>Olay özet olarak gönderilecek mi (yalnızca Error ve Fatal/Critical).</summary>
    /// <param name="logEvent">Olay.</param>
    /// <returns>Gönderilecekse <c>true</c>.</returns>
    public static bool ShouldSend(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        return logEvent.Level >= LogEventLevel.Error;
    }

    /// <summary>Olayı maskelenmiş özete çevirir (exception türü ve mesajı eklenir, yığın izi eklenmez).</summary>
    /// <param name="logEvent">Olay.</param>
    /// <returns>Özet.</returns>
    public static LogSummary ToSummary(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        var message = Konsept.Shared.Logging.KonseptLogging.RenderMessage(logEvent);
        if (logEvent.Exception is not null)
        {
            message = $"{message} ({logEvent.Exception.GetType().Name}: {logEvent.Exception.Message})";
        }

        return new LogSummary(logEvent.Timestamp, logEvent.Level == LogEventLevel.Fatal, SecretMasker.Mask(message));
    }

    /// <summary>Özetlerin gideceği bağlantıyı bağlar (bileşim kökünde, bağlantı oluşturulunca).</summary>
    /// <param name="connection">Sunucu bağlantısı.</param>
    public void Attach(IServerConnection connection) => Volatile.Write(ref _connection, connection);

    /// <inheritdoc />
    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        var connection = Volatile.Read(ref _connection);
        if (connection is null)
        {
            return;
        }

        foreach (var logEvent in batch.Where(ShouldSend))
        {
            await connection.TryReportLogAsync(ToSummary(logEvent), CancellationToken.None).ConfigureAwait(false);
        }
    }
}
