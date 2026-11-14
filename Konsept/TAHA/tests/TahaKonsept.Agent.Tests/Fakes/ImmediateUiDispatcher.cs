using TahaKonsept.Agent.Threading.Base;

namespace TahaKonsept.Agent.Tests.Fakes;

/// <summary>Testlerde her işi çağıran iş parçacığında hemen çalıştıran UI gönderici.</summary>
public sealed class ImmediateUiDispatcher : UiDispatcherBase
{
    /// <inheritdoc />
    protected override bool IsOnUiThread() => true;

    /// <inheritdoc />
    protected override void Enqueue(Action action) => action();
}
