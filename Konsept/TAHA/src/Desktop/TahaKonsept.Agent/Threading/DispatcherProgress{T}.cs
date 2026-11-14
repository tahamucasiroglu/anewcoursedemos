using TahaKonsept.Agent.Threading.Abstract;

namespace TahaKonsept.Agent.Threading;

/// <summary>
/// İlerleme raporlarını <see cref="IUiDispatcher"/> ile UI iş parçacığına taşıyan <see cref="IProgress{T}"/>
/// (<see cref="Progress{T}"/>'in aksine senkronizasyon bağlamına bağlı değildir; testlerde belirleyicidir).
/// </summary>
/// <typeparam name="T">Rapor türü.</typeparam>
public sealed class DispatcherProgress<T> : IProgress<T>
{
    private readonly IUiDispatcher _dispatcher;
    private readonly Action<T> _handler;

    /// <summary>Oluşturur.</summary>
    /// <param name="dispatcher">UI gönderici.</param>
    /// <param name="handler">UI iş parçacığında çalışacak işleyici.</param>
    public DispatcherProgress(IUiDispatcher dispatcher, Action<T> handler)
    {
        _dispatcher = dispatcher;
        _handler = handler;
    }

    /// <inheritdoc />
    public void Report(T value) => _dispatcher.Post(() => _handler(value));
}
