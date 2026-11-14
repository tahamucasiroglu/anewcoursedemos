using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Configuration.Abstract;

namespace TahaKonsept.Agent.Tests.Fakes;

/// <summary>Bellekte tutulan çekirdek ayar deposu (sunucu adresi kaydını doğrulamak için).</summary>
public sealed class InMemoryAgentSettingsStore : IAgentSettingsStore
{
    /// <inheritdoc />
    public event EventHandler<AgentSettings>? Changed;

    /// <inheritdoc />
    public AgentSettings Current { get; private set; } = new() { ServerBaseUrl = "http://192.168.1.102:7111" };

    /// <inheritdoc />
    public AgentSettings Update(Func<AgentSettings, AgentSettings> change)
    {
        Current = change(Current);
        Changed?.Invoke(this, Current);
        return Current;
    }
}
