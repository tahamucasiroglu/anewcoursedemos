using TahaKonsept.Agent.SingleInstance.Abstract;

namespace TahaKonsept.Agent.SingleInstance.Base;

/// <summary>
/// <see cref="ISingleInstanceGuard"/> için ortak taban: olay yayınlama (<see cref="RaiseActivationRequested"/>) ve
/// tek seferlik <see cref="Dispose()"/> kalıbı. Kilit/sinyal mekanizması türetilen sınıftadır.
/// </summary>
public abstract class SingleInstanceGuardBase : ISingleInstanceGuard
{
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler? ActivationRequested;

    /// <inheritdoc />
    public abstract bool TryAcquire();

    /// <inheritdoc />
    public abstract void SignalExistingInstance();

    /// <inheritdoc />
    public abstract void StartListening();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Öne gelme olayını tetikler.</summary>
    protected void RaiseActivationRequested() => ActivationRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Kaynakları bırakır.</summary>
    /// <param name="disposing">Yönetilen kaynaklar da bırakılsın mı.</param>
    protected abstract void Dispose(bool disposing);
}
