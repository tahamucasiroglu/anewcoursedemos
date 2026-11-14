using Microsoft.Extensions.Logging;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Support;

/// <summary>Yazılan log kayıtlarını (seviye + biçimlenmiş mesaj) paylaşılan listede biriktiren test günlükçüsü ("sessiz düşüş yok" testleri için).</summary>
public sealed class ListLogger : ILogger
{
    private readonly List<(LogLevel Level, string Message)> _entries;

    /// <summary>Kayıt listesiyle kurar.</summary>
    /// <param name="entries">Paylaşılan kayıt listesi.</param>
    public ListLogger(List<(LogLevel Level, string Message)> entries)
    {
        _entries = entries;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        lock (_entries)
        {
            _entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
