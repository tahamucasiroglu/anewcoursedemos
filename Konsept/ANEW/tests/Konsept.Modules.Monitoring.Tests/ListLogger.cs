using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Monitoring.Tests;

/// <summary>Testte yazılan logları biriktiren logger.</summary>
/// <typeparam name="T">Kategori tipi.</typeparam>
public sealed class ListLogger<T> : ILogger<T>
{
    /// <summary>Gets yazılan kayıtlar (seviye, metin).</summary>
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        Entries.Add((logLevel, formatter(state, exception)));
    }
}
