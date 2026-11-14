using TahaKonsept.Agent.Settings;
using TahaKonsept.Agent.Settings.Abstract;

namespace TahaKonsept.Agent.Tests.Fakes;

/// <summary>Bellekte tutulan masaüstü ayar deposu.</summary>
public sealed class InMemoryDesktopAppSettingsStore : IDesktopAppSettingsStore
{
    /// <inheritdoc />
    public DesktopAppSettings Current { get; private set; } = new();

    /// <inheritdoc />
    public DesktopAppSettings Update(Func<DesktopAppSettings, DesktopAppSettings> change)
    {
        Current = change(Current);
        return Current;
    }
}
