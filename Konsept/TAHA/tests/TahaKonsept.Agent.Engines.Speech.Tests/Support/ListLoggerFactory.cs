using Microsoft.Extensions.Logging;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Support;

/// <summary>Tüm günlükçüleri tek <see cref="Entries"/> listesine yazan test log fabrikası.</summary>
public sealed class ListLoggerFactory : ILoggerFactory
{
    /// <summary>Yazılan kayıtlar (seviye + mesaj).</summary>
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    /// <summary>Uyarı seviyesindeki mesajlar.</summary>
    public IReadOnlyList<string> Warnings
    {
        get
        {
            lock (Entries)
            {
                return Entries.Where(e => e.Level == LogLevel.Warning).Select(e => e.Message).ToList();
            }
        }
    }

    /// <inheritdoc />
    public void AddProvider(ILoggerProvider provider)
    {
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new ListLogger(Entries);

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
