namespace OkulYonetim.Tests.Integration.Administration;

/// <summary>
/// Eşzamanlılık testinin kapısı (spec 0008 review L-1): açıkken ilk giren istek, ikinci istek aynı noktaya girene kadar
/// (en fazla <see cref="Wait"/>) bekletilir. Okul başına sıra varsa ikinci istek sırada bekler, buraya giremez; sıra yoksa
/// girer ve iki istek kural kontrolünü aynı anda yapar. Böylece sonuç şansa bağlı kalmaz. Bekleme bitince ikincinin
/// girip girmediği <see cref="SecondEnteredWhileFirstWaited"/>'e yazılır.
/// </summary>
public sealed class RaceGate
{
    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(2);

    private readonly TaskCompletionSource firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource secondEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int entered;

    public bool Enabled { get; set; }

    public Task FirstEntered => this.firstEntered.Task;

    public bool? SecondEnteredWhileFirstWaited { get; private set; }

    public async Task PassAsync()
    {
        if (!this.Enabled)
        {
            return;
        }

        int order = Interlocked.Increment(ref this.entered);
        if (order == 1)
        {
            this.firstEntered.TrySetResult();
            Task winner = await Task.WhenAny(this.secondEntered.Task, Task.Delay(Wait)).ConfigureAwait(false);
            this.SecondEnteredWhileFirstWaited = winner == this.secondEntered.Task;
        }
        else if (order == 2)
        {
            this.secondEntered.TrySetResult();
        }
    }
}
