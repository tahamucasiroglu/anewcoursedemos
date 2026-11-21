namespace OkulYonetim.Tests.Integration.Ai;

/// <summary>
/// Gönder/vazgeç yarışının kapısı (spec 0012 review L-2; <c>RaceGate</c> kalıbı): açıkken taslağı ilk okuyan istek (vazgeç),
/// okuduktan sonra test bırakana kadar (en fazla <see cref="Wait"/>) bekletilir; bu sırada test aynı taslakla gönderimi
/// başlatır. Taslak sırası varsa gönderim sırada bekler ve vazgeç önce biter; yoksa gönderim vazgeçin okuması ile silmesi
/// arasında tamamlanır. Böylece sonuç şansa bağlı kalmaz.
/// </summary>
public sealed class DraftHoldGate
{
    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    private readonly TaskCompletionSource held = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int entered;

    public bool Enabled { get; set; }

    public Task Held => this.held.Task;

    public void Release() => this.released.TrySetResult();

    public async Task PassAsync()
    {
        if (!this.Enabled || Interlocked.Increment(ref this.entered) != 1)
        {
            return;
        }

        this.held.TrySetResult();
        await Task.WhenAny(this.released.Task, Task.Delay(Wait)).ConfigureAwait(false);
    }
}
