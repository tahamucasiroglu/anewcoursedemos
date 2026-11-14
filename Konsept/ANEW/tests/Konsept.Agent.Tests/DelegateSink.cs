using Serilog.Core;
using Serilog.Events;

namespace Konsept.Agent.Tests;

/// <summary>Testte log olaylarını bir eyleme ileten sink.</summary>
/// <param name="emit">Eylem.</param>
public sealed class DelegateSink(Action<LogEvent> emit) : ILogEventSink
{
    /// <inheritdoc />
    public void Emit(LogEvent logEvent) => emit(logEvent);
}
