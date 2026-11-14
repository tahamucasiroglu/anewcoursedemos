using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Settings;

namespace Konsept.Agent;

/// <summary>
/// Ajanın bileşim kökü: ayarlar, ajan bilgisi, sunucu bağlantısı ve tarayıcı açıcı.
/// </summary>
/// <param name="Settings">Ayarlar.</param>
/// <param name="Info">Ajan bilgisi.</param>
/// <param name="Connection">Sunucu bağlantısı.</param>
/// <param name="Browser">Tarayıcı açıcı.</param>
internal sealed record AgentComposition(AgentSettings Settings, IAgentInfoProvider Info, IServerConnection Connection, IBrowserLauncher Browser)
{
    /// <summary>Gerçek bileşenlerle kurar (ayar dosyası ajan klasöründen).</summary>
    /// <returns>Bileşim.</returns>
    public static AgentComposition CreateDefault()
    {
        var settings = AgentSettingsLoader.Load(AppContext.BaseDirectory);
        var info = new AssemblyAgentInfoProvider(typeof(App).Assembly);
        return new AgentComposition(settings, info, new SignalRServerConnection(settings, info), new ShellBrowserLauncher());
    }
}
