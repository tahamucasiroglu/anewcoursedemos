using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Logging;
using Konsept.Agent.Infrastructure.Settings;

namespace Konsept.Agent;

/// <summary>
/// Ajanın bileşim kökü: ayarlar, ajan bilgisi, sunucu bağlantısı, tarayıcı açıcı ve teknik log (spec 0008).
/// </summary>
/// <param name="Settings">Ayarlar.</param>
/// <param name="Info">Ajan bilgisi.</param>
/// <param name="Connection">Sunucu bağlantısı.</param>
/// <param name="Browser">Tarayıcı açıcı.</param>
/// <param name="Logging">Teknik log; testlerde <c>null</c>.</param>
internal sealed record AgentComposition(
    AgentSettings Settings,
    IAgentInfoProvider Info,
    IServerConnection Connection,
    IBrowserLauncher Browser,
    AgentLogging? Logging = null)
{
    /// <summary>Gerçek bileşenlerle kurar (ayar dosyası ajan klasöründen).</summary>
    /// <returns>Bileşim.</returns>
    public static AgentComposition CreateDefault()
    {
        var logging = AgentLogging.Create(AppContext.BaseDirectory, TimeProvider.System);
        Serilog.Log.Logger = logging.Logger;
        var settings = AgentSettingsLoader.Load(AppContext.BaseDirectory);
        var info = new AssemblyAgentInfoProvider(typeof(App).Assembly);
        var connection = new SignalRServerConnection(settings, info, logger: logging.Logger);
        logging.SummarySink.Attach(connection);
        logging.Logger.Information("Ajan başladı: sürüm {Version}, sunucu {Server}", info.Version, settings.ServerUrl);
        return new AgentComposition(settings, info, connection, new ShellBrowserLauncher(), logging);
    }
}
