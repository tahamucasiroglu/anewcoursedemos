using TahaKonsept.Agent.Threading.Abstract;

namespace TahaKonsept.Agent.Threading.Base;

/// <summary>
/// <see cref="IUiDispatcher"/> tabanı: zaten UI iş parçacığındaysa işi hemen çalıştırır, değilse <see cref="Enqueue"/> ile kuyruğa verir.
/// </summary>
public abstract class UiDispatcherBase : IUiDispatcher
{
    /// <inheritdoc />
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (IsOnUiThread())
        {
            action();
        }
        else
        {
            Enqueue(action);
        }
    }

    /// <summary>Çağıran UI iş parçacığında mı.</summary>
    /// <returns>Oradaysa <c>true</c>.</returns>
    protected abstract bool IsOnUiThread();

    /// <summary>İşi UI kuyruğuna ekler.</summary>
    /// <param name="action">İş.</param>
    protected abstract void Enqueue(Action action);
}
