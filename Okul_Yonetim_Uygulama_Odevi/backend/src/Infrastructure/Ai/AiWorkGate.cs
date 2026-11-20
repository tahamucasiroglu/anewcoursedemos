using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Ai;

/// <summary>
/// Tek ekran kartı için AI iş sırası (spec 0012, plan Risks 3): aynı anda yalnızca bir model çağrısı (ses ya da metin)
/// çalışır; sıradaki istek en fazla verilen süre bekler, aşarsa <see cref="AiUnavailableException"/> (<c>busy</c>) —
/// kullanıcı "kullanılamıyor" görür, istek askıda kalmaz.
/// </summary>
public sealed class AiWorkGate : IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly TimeSpan queueTimeout;

    public AiWorkGate(TimeSpan queueTimeout)
    {
        this.queueTimeout = queueTimeout;
    }

    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);
        if (!await this.semaphore.WaitAsync(this.queueTimeout, cancellationToken).ConfigureAwait(false))
        {
            throw new AiUnavailableException("busy");
        }

        try
        {
            return await work(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            this.semaphore.Release();
        }
    }

    public void Dispose() => this.semaphore.Dispose();
}
