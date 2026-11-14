namespace TahaKonsept.Agent.Core.Execution;

/// <summary>
/// Bir işin iptal/duraklatma denetimi: iş başına <see cref="CancellationTokenSource"/>, durma nedeni ve çalışan adım sayısı.
/// Duraklatılan/iptal edilen işe yeni komut gelirse <c>JobRunner</c> yeni bir denetim oluşturur (devam).
/// </summary>
public sealed class JobControl
{
    private readonly CancellationTokenSource _cts = new();
    private int _reason;
    private int _running;

    /// <summary>İşin iptal belirteci.</summary>
    public CancellationToken Token => _cts.Token;

    /// <summary>Durma nedeni.</summary>
    public JobStopReason Reason => (JobStopReason)Volatile.Read(ref _reason);

    /// <summary>Durduruldu mu.</summary>
    public bool IsStopped => Reason != JobStopReason.None;

    /// <summary>Şu an çalışan adım sayısı.</summary>
    public int RunningCount => Volatile.Read(ref _running);

    /// <summary>İşi durdurur (iptal, duraklatmayı ezer) ve belirteci iptal eder.</summary>
    /// <param name="reason">Neden.</param>
    public void Stop(JobStopReason reason)
    {
        if (reason == JobStopReason.Cancelled)
        {
            Volatile.Write(ref _reason, (int)reason);
        }
        else
        {
            Interlocked.CompareExchange(ref _reason, (int)reason, (int)JobStopReason.None);
        }

        _cts.Cancel();
    }

    /// <summary>Çalışan adım sayısını artırır.</summary>
    public void EnterRunning() => Interlocked.Increment(ref _running);

    /// <summary>Çalışan adım sayısını azaltır.</summary>
    /// <returns>Kalan çalışan adım sayısı.</returns>
    public int ExitRunning() => Interlocked.Decrement(ref _running);
}
