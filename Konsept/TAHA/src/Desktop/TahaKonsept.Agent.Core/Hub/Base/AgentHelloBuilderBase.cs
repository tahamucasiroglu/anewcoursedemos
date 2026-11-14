using System.Runtime.InteropServices;
using TahaKonsept.Agent.Core.Capabilities.Abstract;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Agent.Core.Networking.Abstract;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Hub.Base;

/// <summary>
/// <see cref="AgentHello"/> üretici tabanı: ayarlar (port, akış anahtarı), LAN adresi ve algılanan yetenekleri birleştirir.
/// Sürüm <see cref="AgentVersion"/> ile türetilen sınıftan gelir.
/// </summary>
public abstract class AgentHelloBuilderBase : IAgentHelloBuilder
{
    private readonly IAgentSettingsStore _settings;
    private readonly ILanAddressProvider _lan;
    private readonly ICapabilityDetector _capabilities;

    /// <summary>Oluşturur.</summary>
    /// <param name="settings">Ayar deposu.</param>
    /// <param name="lan">LAN adresi bulucu.</param>
    /// <param name="capabilities">Yetenek algılayıcı.</param>
    protected AgentHelloBuilderBase(IAgentSettingsStore settings, ILanAddressProvider lan, ICapabilityDetector capabilities)
    {
        _settings = settings;
        _lan = lan;
        _capabilities = capabilities;
    }

    /// <summary>Ajan sürümü.</summary>
    protected abstract string AgentVersion { get; }

    /// <inheritdoc />
    public async Task<AgentHello> BuildAsync(CancellationToken ct)
    {
        var settings = _settings.Current;
        return new AgentHello
        {
            AgentVersion = AgentVersion,
            MachineName = Environment.MachineName,
            OsDescription = RuntimeInformation.OSDescription,
            LanIpAddress = _lan.GetLanIpAddress(),
            VideoServerPort = settings.VideoServerPort,
            StreamKey = settings.StreamKey,
            Capabilities = await _capabilities.DetectAsync(ct),
        };
    }
}
