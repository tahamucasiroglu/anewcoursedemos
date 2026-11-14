using Serilog.Core;
using Serilog.Events;

namespace Konsept.Shared.Logging;

/// <summary>Olayın zamanını UTC olarak <c>UtcTimestamp</c> özelliğine koyar (spec 0008: zamanlar UTC saklanır).</summary>
public sealed class UtcTimestampEnricher : ILogEventEnricher
{
    /// <summary>Özellik adı.</summary>
    public const string PropertyName = "UtcTimestamp";

    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        logEvent.AddOrUpdateProperty(new LogEventProperty(PropertyName, new ScalarValue(logEvent.Timestamp.UtcDateTime)));
    }
}
