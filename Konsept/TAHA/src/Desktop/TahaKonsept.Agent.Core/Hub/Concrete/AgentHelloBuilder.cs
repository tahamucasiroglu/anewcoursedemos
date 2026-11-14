using TahaKonsept.Agent.Core.Capabilities.Abstract;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Hub.Base;
using TahaKonsept.Agent.Core.Networking.Abstract;

namespace TahaKonsept.Agent.Core.Hub.Concrete;

/// <summary><c>Hello</c> üretici; sürümü <see cref="AgentOptions.AgentVersion"/>'dan alır.</summary>
public sealed class AgentHelloBuilder : AgentHelloBuilderBase
{
    private readonly AgentOptions _options;

    /// <summary>Oluşturur.</summary>
    /// <param name="settings">Ayar deposu.</param>
    /// <param name="lan">LAN adresi bulucu.</param>
    /// <param name="capabilities">Yetenek algılayıcı.</param>
    /// <param name="options">Seçenekler.</param>
    public AgentHelloBuilder(IAgentSettingsStore settings, ILanAddressProvider lan, ICapabilityDetector capabilities, AgentOptions options)
        : base(settings, lan, capabilities)
    {
        _options = options;
    }

    /// <inheritdoc />
    protected override string AgentVersion => _options.AgentVersion;
}
