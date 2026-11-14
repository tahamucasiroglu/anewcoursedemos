using Avalonia.Threading;
using TahaKonsept.Agent.Threading.Base;

namespace TahaKonsept.Agent.Threading.Concrete;

/// <summary>Avalonia <see cref="Dispatcher.UIThread"/> üzerinden çalışan UI gönderici.</summary>
public sealed class AvaloniaUiDispatcher : UiDispatcherBase
{
    /// <inheritdoc />
    protected override bool IsOnUiThread() => Dispatcher.UIThread.CheckAccess();

    /// <inheritdoc />
    protected override void Enqueue(Action action) => Dispatcher.UIThread.Post(action);
}
