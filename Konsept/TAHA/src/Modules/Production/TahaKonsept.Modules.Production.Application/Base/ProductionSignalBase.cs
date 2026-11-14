using TahaKonsept.Modules.Production.Application.Abstract;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IProductionSignal"/> ortak davranışı: tek izinli semafor; art arda gelen bildirimler tek uyanmaya birleşir.
/// </summary>
public abstract class ProductionSignalBase : IProductionSignal, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(0, 1);

    /// <summary>Orkestratörü uyandırır.</summary>
    public void Notify()
    {
        try
        {
            if (_semaphore.CurrentCount == 0)
            {
                _semaphore.Release();
            }
        }
        catch (SemaphoreFullException)
        {
            // Zaten uyandırılmış; birleşen bildirim.
        }
    }

    /// <summary>Sinyal ya da zaman aşımına kadar bekler.</summary>
    /// <param name="timeout">Azami bekleme.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) => _semaphore.WaitAsync(timeout, cancellationToken);

    /// <summary>Semaforu bırakır.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Kaynakları bırakır.</summary>
    /// <param name="disposing">Yönetilen kaynaklar bırakılsın mı.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _semaphore.Dispose();
        }
    }
}
