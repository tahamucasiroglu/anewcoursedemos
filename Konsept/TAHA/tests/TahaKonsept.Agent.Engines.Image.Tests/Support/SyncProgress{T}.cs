namespace TahaKonsept.Agent.Engines.Image.Tests.Support;

/// <summary>Raporları anında listeye ekleyen eşzamanlı <see cref="IProgress{T}"/> (Progress&lt;T&gt; gecikmesiz).</summary>
/// <typeparam name="T">Rapor türü.</typeparam>
public sealed class SyncProgress<T> : IProgress<T>
{
    private readonly List<T> _items;

    /// <summary>Hedef listeyle oluşturur.</summary>
    /// <param name="items">Raporların ekleneceği liste.</param>
    public SyncProgress(List<T> items)
    {
        _items = items;
    }

    /// <inheritdoc />
    public void Report(T value)
    {
        lock (_items)
        {
            _items.Add(value);
        }
    }
}
