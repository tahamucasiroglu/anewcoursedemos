namespace TahaKonsept.Agent.Core.Execution;

/// <summary>
/// Raporu aynı iş parçacığında hemen ileten <see cref="IProgress{T}"/> (<see cref="Progress{T}"/> gibi senkronizasyon bağlamına
/// gönderip sırayı bozmaz). Motor ilerlemesini aşama ilerlemesine çevirmek için kullanılır.
/// </summary>
/// <typeparam name="T">Rapor türü.</typeparam>
public sealed class InlineProgress<T> : IProgress<T>
{
    private readonly Action<T> _handler;

    /// <summary>Oluşturur.</summary>
    /// <param name="handler">Rapor işleyici.</param>
    public InlineProgress(Action<T> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <inheritdoc />
    public void Report(T value) => _handler(value);
}
