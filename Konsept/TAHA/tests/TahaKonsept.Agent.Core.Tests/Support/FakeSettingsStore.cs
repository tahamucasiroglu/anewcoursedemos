using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Configuration.Abstract;

namespace TahaKonsept.Agent.Core.Tests.Support;

/// <summary>Bellek içi ayar deposu (testler için).</summary>
public sealed class FakeSettingsStore : IAgentSettingsStore
{
    /// <summary>Oluşturur.</summary>
    /// <param name="settings">Başlangıç ayarları.</param>
    public FakeSettingsStore(AgentSettings settings)
    {
        Current = settings;
    }

    /// <inheritdoc />
    public event EventHandler<AgentSettings>? Changed;

    /// <inheritdoc />
    public AgentSettings Current { get; private set; }

    /// <inheritdoc />
    public AgentSettings Update(Func<AgentSettings, AgentSettings> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        Current = change(Current);
        Changed?.Invoke(this, Current);
        return Current;
    }
}
