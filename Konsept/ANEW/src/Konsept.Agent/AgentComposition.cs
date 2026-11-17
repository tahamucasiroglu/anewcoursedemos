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
using Konsept.Agent.Infrastructure.Update;

namespace Konsept.Agent;

/// <summary>
/// Ajanın bileşim kökü: ayarlar, ajan bilgisi, sunucu bağlantısı, tarayıcı açıcı, teknik log (spec 0008) ve oturum akışı
/// (spec 0012: eşleştirme kodu, saklı belirteç, oturum kapandı), hata bildirimi (spec 0022), ilk kurulum (spec 0009) ve yerel üretim aşamaları
/// + yerel video akışı (spec 0017).
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
/// <param name="Updater">Ajan güncellemesi (spec 0023); verilmezse "Güncellemeleri denetle" yalnızca bilgi verir.</param>
internal sealed record AgentComposition(
    AgentSettings Settings,
    IAgentInfoProvider Info,
    IServerConnection Connection,
    IBrowserLauncher Browser,
    AgentLogging? Logging = null,
    IAgentSessionCoordinator? Session = null,
    IErrorReportCenter? Reports = null,
    IDesktopNotifier? Notifier = null,
    ISetupCoordinator? Setup = null,
    IAgentUpdater? Updater = null)
{
    /// <summary>Güncelleme açık anahtarının AssemblyMetadata anahtarı (spec 0023).</summary>
    public const string UpdatePublicKeyMetadata = "AgentUpdatePublicKey";

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
        var setupState = new FileSetupStateStore(root);
        var videoFolder = () => setupState.Load().VideoFolder ?? AgentPaths.DefaultVideoFolder;
        var streams = new LocalVideoServer(videoFolder, TimeProvider.System, logging.Logger);
        streams.Start(); // spec 0017 Req 21: yalnızca 127.0.0.1.
        var renderer = new SandboxedSceneRenderer(root, settings.Render ?? throw new InvalidOperationException("Render sınırları yüklenemedi."), logging.Logger);
        var stages = new ProcessStageRunner(setupState, root, videoFolder, renderer, logging.Logger);
        var setupHost = new WindowsSetupHost(new HttpClient());
        var setup = new SetupCoordinator(
            ComponentCatalog.Load(),
            setupHost,
            setupState,
            root,
            AgentPaths.DefaultVideoFolder,
            new Uri(settings.SiteUrl, "ajan-bilesenleri/"),
            logging.Logger);
        var connection = new SignalRServerConnection(
            settings,
            info,
            logger: logging.Logger,
            accessToken: tokens.Read,
            stages: stages,
            streams: streams,
            installedComponents: () => [.. setupState.Load().Installed.Keys],
            files: new ShellVideoFileActions(videoFolder),
            deleter: new VideoFileDeleter(videoFolder),
            installer: new ComponentInstaller(setup, TimeProvider.System));

        // Kurulum bitince kurulu bileşen listesi sunucuya yeniden bildirilir (spec 0027 Req 9, B-149: eskiden yalnızca kayıtta).
        setup.Changed += (_, change) =>
        {
            if (change.Kind == Domain.Enums.SetupEventKind.Ready)
            {
                _ = connection.TryReportComponentsAsync(CancellationToken.None);
            }
        };
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
        var updater = new AgentUpdater(
            info.Version,
            new Uri(settings.SiteUrl, "ajan-guncelleme/"),
            setupHost,
            new Ed25519ManifestVerifier(AgentSettingsLoader.ReadMetadata(typeof(App).Assembly).GetValueOrDefault(UpdatePublicKeyMetadata)),
            CreateInstallation(),
            connection,
            TimeProvider.System,
            logging.Logger);
        logging.Logger.Information("Ajan başladı: sürüm {Version}, sunucu {Server}", info.Version, settings.ServerUrl);
        return new AgentComposition(settings, info, connection, new ShellBrowserLauncher(), logging, session, reports, Setup: setup, Updater: updater);
    }

    /// <summary>Kurulu kopyayı (spec 0023; HKCU kayıtları, Başlat menüsü) gerçek Windows bileşenleriyle kurar.</summary>
    /// <returns>Kurulu kopya.</returns>
    public static AgentInstallation CreateInstallation() =>
        new(AgentPaths.InstallRoot, Environment.GetFolderPath(Environment.SpecialFolder.Programs), new WindowsInstallHost());

    /// <summary>
    /// Ajan siteden indirilen exe olarak mı çalışıyor (<c>anewconcept-ajan*.exe</c>; yalnızca o zaman kendini kurulum yerine kopyalar ve kaydeder,
    /// B-77); geliştirme derlemesinde (<c>Konsept.Agent.exe</c>, testler) <c>false</c>.
    /// </summary>
    /// <param name="processPath">Çalışan exe.</param>
    /// <returns>Yayın exe'si mi.</returns>
    public static bool IsReleaseExecutable(string? processPath) =>
        Path.GetFileName(processPath ?? string.Empty).StartsWith("anewconcept-ajan", StringComparison.OrdinalIgnoreCase);
}
