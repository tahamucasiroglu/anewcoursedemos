using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.Agent.Core.Hosting.Base;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Agent.Core.Pairing.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.Agent.Core.VideoServer.Abstract;

namespace TahaKonsept.Agent.Core.Hosting.Concrete;

/// <summary>Loglayan ajan ana makinesi.</summary>
public sealed class AgentHost : AgentHostBase
{
    private readonly AgentOptions _options;
    private readonly ILogger<AgentHost> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="hub">Hub bağlantısı.</param>
    /// <param name="pairing">Eşleme servisi.</param>
    /// <param name="prerequisites">Kurulum yöneticisi.</param>
    /// <param name="videoServer">Video sunucusu.</param>
    /// <param name="jobs">İş çalıştırıcı.</param>
    /// <param name="settings">Ayar deposu.</param>
    /// <param name="options">Seçenekler.</param>
    /// <param name="logger">Logger.</param>
    public AgentHost(
        IAgentHubConnection hub,
        IDevicePairingService pairing,
        IPrerequisiteInstaller prerequisites,
        IVideoServer videoServer,
        IJobRunner jobs,
        IAgentSettingsStore settings,
        AgentOptions options,
        ILogger<AgentHost> logger)
        : base(hub, pairing, prerequisites, videoServer, jobs, settings, options)
    {
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override void OnStarting(AgentSettings settings) =>
        _logger.LogInformation("Taha Konsept ajanı {Version} başlıyor; sunucu {Server}, video portu {Port}.", _options.AgentVersion, settings.ServerBaseUrl, settings.VideoServerPort);

    /// <inheritdoc />
    protected override void OnStopped() => _logger.LogInformation("Taha Konsept ajanı durdu.");

    /// <inheritdoc />
    protected override void OnBackgroundError(Exception exception) =>
        _logger.LogWarning(exception, "Ajan arka plan işleminde hata.");
}
