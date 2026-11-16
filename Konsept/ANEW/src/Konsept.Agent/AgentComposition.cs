using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Application.Base;
using Konsept.Agent.Application.Concrete;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Logging;
using Konsept.Agent.Infrastructure.Settings;

namespace Konsept.Agent;

/// <summary>
/// Ajanın bileşim kökü: ayarlar, ajan bilgisi, sunucu bağlantısı, tarayıcı açıcı, teknik log (spec 0008) ve oturum akışı
/// (spec 0012: eşleştirme kodu, saklı belirteç, oturum kapandı).
/// </summary>
/// <param name="Settings">Ayarlar.</param>
/// <param name="Info">Ajan bilgisi.</param>
/// <param name="Connection">Sunucu bağlantısı.</param>
/// <param name="Browser">Tarayıcı açıcı.</param>
/// <param name="Logging">Teknik log; testlerde <c>null</c>.</param>
/// <param name="Session">Oturum akışı; verilmezse bağlantı doğrudan başlar (eski testler).</param>
internal sealed record AgentComposition(
    AgentSettings Settings,
    IAgentInfoProvider Info,
    IServerConnection Connection,
    IBrowserLauncher Browser,
    AgentLogging? Logging = null,
    IAgentSessionCoordinator? Session = null)
{
    /// <summary>Kod durumu sorgu aralığı.</summary>
    public static readonly TimeSpan PairingPollInterval = TimeSpan.FromSeconds(2);

    /// <summary>Gerçek bileşenlerle kurar (ayar dosyası ve oturum belirteci ajan klasöründen).</summary>
    /// <returns>Bileşim.</returns>
    public static AgentComposition CreateDefault()
    {
        var logging = AgentLogging.Create(AppContext.BaseDirectory, TimeProvider.System);
        Serilog.Log.Logger = logging.Logger;
        var settings = AgentSettingsLoader.Load(AppContext.BaseDirectory);
        var info = new AssemblyAgentInfoProvider(typeof(App).Assembly);
        var tokens = new FileAgentTokenStore(AppContext.BaseDirectory);
        var connection = new SignalRServerConnection(settings, info, logger: logging.Logger, accessToken: tokens.Read);
        logging.SummarySink.Attach(connection);
        var session = new AgentSessionCoordinator(
            connection,
            new HttpPairingClient(settings),
            tokens,
            info,
            new AgentSessionOptions(PairingPollInterval, settings.ReconnectDelay),
            TimeProvider.System);
        logging.Logger.Information("Ajan başladı: sürüm {Version}, sunucu {Server}", info.Version, settings.ServerUrl);
        return new AgentComposition(settings, info, connection, new ShellBrowserLauncher(), logging, session);
    }
}
