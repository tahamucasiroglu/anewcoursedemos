using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Application.Base;

namespace Konsept.Agent.Application.Concrete;

/// <summary>Eksik bileşen kurulumu (spec 0027); bekleme <see cref="TimeProvider"/>'dan.</summary>
/// <param name="setup">Kurulum motoru.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
public sealed class ComponentInstaller(ISetupCoordinator setup, TimeProvider timeProvider) : ComponentInstallerBase(setup)
{
    /// <inheritdoc />
    protected override Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, timeProvider, cancellationToken);
}
