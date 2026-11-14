namespace TahaKonsept.Agent.Engines.Video.Tests.Support;

/// <summary>Senkron <see cref="IProgress{T}"/> (Progress&lt;T&gt; gibi senkronizasyon bağlamına göndermez; test sırası kesin).</summary>
/// <param name="onReport">Her bildirimde çağrılır.</param>
public sealed class SyncProgress(Action<double> onReport) : IProgress<double>
{
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public void Report(double value)
    {
        lock (_gate)
        {
            onReport(value);
        }
    }
}
