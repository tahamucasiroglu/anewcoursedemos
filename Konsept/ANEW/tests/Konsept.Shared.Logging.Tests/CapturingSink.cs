using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace Konsept.Shared.Logging.Tests;

/// <summary>Testte yazılan log olaylarını biriktiren sink.</summary>
public sealed class CapturingSink : ILogEventSink
{
    /// <summary>Gets yakalanan olaylar.</summary>
    public ConcurrentQueue<LogEvent> Events { get; } = new();

    /// <inheritdoc />
    public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);
}
