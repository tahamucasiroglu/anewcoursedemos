using CommunityToolkit.Mvvm.ComponentModel;

namespace TahaKonsept.Agent.ViewModels.Base;

/// <summary>
/// Tüm görünüm modellerinin tabanı: <see cref="ObservableObject"/> (özellik bildirimi, <c>SetProperty</c>) ve tek seferlik
/// <see cref="Dispose()"/> (olay aboneliklerini bırakmak için <see cref="OnDisposing"/>). Avalonia türlerine bağlı değildir (test edilebilir).
/// </summary>
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private bool _disposed;

    /// <summary>Görünüm modeli bırakıldı mı.</summary>
    protected bool IsDisposed => _disposed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        OnDisposing();
        GC.SuppressFinalize(this);
    }

    /// <summary>Bırakılırken çağrılır (olay aboneliklerini kaldır, iptal et).</summary>
    protected virtual void OnDisposing()
    {
    }
}
