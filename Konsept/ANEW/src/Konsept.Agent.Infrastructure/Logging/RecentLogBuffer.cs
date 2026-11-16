using System.Collections.Concurrent;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Shared.Kernel.Security;
using Serilog.Core;
using Serilog.Events;

namespace Konsept.Agent.Infrastructure.Logging;

/// <summary>
/// Ajanın son Warning+ log satırlarını bellekte tutar (en fazla <see cref="Capacity"/>; spec 0022 Req 4 — rapor bunlardan son 15 dakikayı
/// alır). Metin maskelenir; exception türü ve mesajı eklenir, yığın izi eklenmez. Çerçeve adaptörü (Serilog sink'i).
/// </summary>
public sealed class RecentLogBuffer : ILogEventSink, IRecentLogSource
{
    /// <summary>Tutulan en fazla satır.</summary>
    public const int Capacity = 1000;

    private readonly ConcurrentQueue<AgentLogLine> _lines = new();

    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        if (logEvent.Level < LogEventLevel.Warning)
        {
            return;
        }

        var text = Konsept.Shared.Logging.KonseptLogging.RenderMessage(logEvent);
        if (logEvent.Exception is not null)
        {
            text = $"{text} ({logEvent.Exception.GetType().Name}: {logEvent.Exception.Message})";
        }

        _lines.Enqueue(new AgentLogLine(logEvent.Timestamp, LevelOf(logEvent.Level), SecretMasker.Mask(text)));
        while (_lines.Count > Capacity && _lines.TryDequeue(out _))
        {
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentLogLine> Snapshot() => [.. _lines];

    private static AgentLogLevel LevelOf(LogEventLevel level) => level switch
    {
        LogEventLevel.Warning => AgentLogLevel.Warning,
        LogEventLevel.Error => AgentLogLevel.Error,
        LogEventLevel.Fatal => AgentLogLevel.Critical,
        _ => AgentLogLevel.Information,
    };
}
