using System.Runtime.InteropServices;
using Konsept.Agent.Abstract;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Application.Base;
using Konsept.Agent.Application.Concrete;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Logging;
using Konsept.Agent.Infrastructure.Settings;
using Konsept.Agent.Infrastructure.Setup;

namespace Konsept.Agent;

/// <summary>
/// Ajanın bileşim kökü: ayarlar, ajan bilgisi, sunucu bağlantısı, tarayıcı açıcı, teknik log (spec 0008) ve oturum akışı
/// (spec 0012: eşleştirme kodu, saklı belirteç, oturum kapandı), hata bildirimi (spec 0022) ve ilk kurulum (spec 0009).
/// </summary>
/// <param name="Settings">Ayarlar.</param>
/// <param name="Info">Ajan bilgisi.</param>
/// <param name="Connection">Sunucu bağlantısı.</param>
/// <param name="Browser">Tarayıcı açıcı.</param>
/// <param name="Logging">Teknik log; testlerde <c>null</c>.</param>
/// <param name="Session">Oturum akışı; verilmezse bağlantı doğrudan başlar (eski testler).</param>
/// <param name="Reports">Hata bildirimi merkezi (spec 0022); testlerde verilmeyebilir.</param>
/// <param name="Notifier">Windows bildirimi (spec 0022); testlerde sahte.</param>
/// <param name="Setup">İlk kurulum (spec 0009); verilmezse kurulum ekranı açılmaz (eski testler).</param>
internal sealed record AgentComposition(
    AgentSettings Settings,
    IAgentInfoProvider Info,
    IServerConnection Connection,
    IBrowserLauncher Browser,
    AgentLogging? Logging = null,
    IAgentSessionCoordinator? Session = null,
    IErrorReportCenter? Reports = null,
    IDesktopNotifier? Notifier = null,
    ISetupCoordinator? Setup = null)
{
    /// <summary>Kod durumu sorgu aralığı.</summary>
    public static readonly TimeSpan PairingPollInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gerçek bileşenlerle kurar. Ayar dosyası ajan exe'sinin klasöründen (yoksa yayında gömülü adresler); log, oturum belirteci,
    /// bekleyen bildirimler ve kurulum durumu kullanıcı başına <c>%LOCALAPPDATA%\anewconcept</c> altında (spec 0009 Req 6, 22).
    /// </summary>
    /// <returns>Bileşim.</returns>
    public static AgentComposition CreateDefault()
    {
        var root = AgentPaths.InstallRoot;
        Directory.CreateDirectory(root);
        var logging = AgentLogging.Create(root, TimeProvider.System);
        Serilog.Log.Logger = logging.Logger;
        var settings = AgentSettingsLoader.Load(AppContext.BaseDirectory, AgentSettingsLoader.ReadMetadata(typeof(App).Assembly));
        var info = new AssemblyAgentInfoProvider(typeof(App).Assembly);
        var tokens = new FileAgentTokenStore(root);
        var connection = new SignalRServerConnection(settings, info, logger: logging.Logger, accessToken: tokens.Read);
        logging.SummarySink.Attach(connection);
        var session = new AgentSessionCoordinator(
            connection,
            new HttpPairingClient(settings),
            tokens,
            info,
            new AgentSessionOptions(PairingPollInterval, settings.ReconnectDelay),
            TimeProvider.System);
        var reports = new ErrorReportCenter(
            connection,
            new FileErrorReportQueue(root),
            logging.RecentLogs,
            info,
            new ReportEnvironment(RuntimeInformation.OSDescription, Environment.MachineName, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
            TimeProvider.System,
            logging.Logger);
        logging.AlertSink.Attach(reports);
        var setup = new SetupCoordinator(
            ComponentCatalog.Load(),
            new WindowsSetupHost(new HttpClient()),
            new FileSetupStateStore(root),
            root,
            AgentPaths.DefaultVideoFolder,
            new Uri(settings.SiteUrl, "ajan-bilesenleri/"),
            logging.Logger);
        logging.Logger.Information("Ajan başladı: sürüm {Version}, sunucu {Server}", info.Version, settings.ServerUrl);
        return new AgentComposition(settings, info, connection, new ShellBrowserLauncher(), logging, session, reports, Setup: setup);
    }
}
