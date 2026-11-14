using Microsoft.Extensions.Logging;

namespace TahaKonsept.Agent.Engines.Video.Tests.Support;

/// <summary>Log kayıtlarını (seviye + mesaj) biriktiren test günlükçüsü; GPU'dan CPU'ya düşüşün uyarı olarak loglandığını doğrulamak için.</summary>
/// <typeparam name="T">Kategori türü.</typeparam>
public sealed class ListLogger<T> : ILogger<T>
{
    /// <summary>Yazılan kayıtlar.</summary>
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    /// <summary>Uyarı mesajları.</summary>
    public IReadOnlyList<string> Warnings => Entries.Where(e => e.Level == LogLevel.Warning).Select(e => e.Message).ToList();

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        lock (Entries)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
