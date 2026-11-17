using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Konsept.Agent.Abstract;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Application.Base;
using Konsept.Agent.Concrete;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Resources;

namespace Konsept.Agent;

/// <summary>
/// Ajan uygulaması: tray ikonu ve menüsü ("Siteye git", "Güncellemeleri denetle", "Çıkış"), durum penceresi ve sunucu bağlantısı
/// (spec 0001 Req 6–7, AC-5, AC-6). Uygulama yalnızca "Çıkış" ile kapanır. Hata uyarısı (spec 0022 Req 4): uyarı penceresi öne gelir,
/// Windows bildirimi gösterilir, tray ikonunda bekleyen bildirim işareti ve menüde "Hata uyarısını göster" görünür.
/// </summary>
public sealed partial class App : Avalonia.Application
{
    /// <summary>Uyarı örneği açan komut satırı argümanı (kullanıcı testi için).</summary>
    internal const string TestErrorArgument = "--hata-dene";

    private const string TrayIconUri = "avares://Konsept.Agent/Assets/tray-icon.png";
    private const string TrayAlertIconUri = "avares://Konsept.Agent/Assets/tray-icon-alert.png";
    private TrayIcon? _trayIcon;
    private IErrorReportCenter? _reports;
    private IDesktopNotifier? _notifier;
    private int _pending;
    private readonly CancellationTokenSource _updateStop = new();
    private ISetupCoordinator? _setup;
    private CancellationTokenSource? _setupStop;
    private IAgentUpdater? _updater;
    private IDesktopNotifier? _updateNotifier;

    /// <summary>Gets or sets bileşim (testte sahte bileşenlerle değiştirilebilir).</summary>
    internal AgentComposition? Composition { get; set; }

    /// <summary>Gets or sets uygulamayı sonlandıran eylem (masaüstünde lifetime.Shutdown; testte sahte).</summary>
    internal Action? ShutdownAction { get; set; }

    /// <summary>Gets tray menüsü.</summary>
    internal NativeMenu? TrayMenu { get; private set; }

    /// <summary>Gets tray menüsünün durum başlığı (tıklanamaz).</summary>
    internal NativeMenuItem? TrayStatusItem { get; private set; }

    /// <summary>Gets tray menüsündeki "Hata uyarısını göster" satırı (yalnızca uyarı ya da bekleyen bildirim varken görünür).</summary>
    internal NativeMenuItem? TrayPendingItem { get; private set; }

    /// <summary>Gets açık hata uyarısı penceresi; yoksa <c>null</c>.</summary>
    internal ErrorAlertWindow? AlertWindow { get; private set; }

    /// <summary>Gets tray menüsündeki "Güncellemeleri denetle" satırı (spec 0023 Req 1, 12).</summary>
    internal NativeMenuItem? TrayCheckUpdatesItem { get; private set; }

    /// <summary>Gets tray menüsündeki "Yeni sürüm hazır: X — Şimdi güncelle" satırı (yalnızca yeni sürüm varken görünür).</summary>
    internal NativeMenuItem? TrayUpdateItem { get; private set; }

    /// <summary>Gets açık güncelleme ekranı; açılmadıysa <c>null</c> (spec 0023).</summary>
    internal UpdateWindow? UpdateWindow { get; private set; }

    /// <summary>Gets açık kurulum ekranı; kurulum bittiyse ya da gerekmediyse <c>null</c> (spec 0009).</summary>
    internal SetupWindow? SetupWindow { get; private set; }

    /// <summary>Gets tray ikonunun ipucu (testler okur).</summary>
    internal string? TrayToolTip => _trayIcon?.ToolTipText;

    /// <summary>Gets a value indicating whether tray ikonunda bekleyen bildirim işareti var.</summary>
    internal bool TrayMarkerShown { get; private set; }

    /// <summary>Masaüstü yaşam döngüsünü ayarlar: pencere kapanınca uygulama kapanmaz.</summary>
    /// <param name="desktop">Masaüstü yaşam döngüsü.</param>
    internal static void ConfigureDesktop(IClassicDesktopStyleApplicationLifetime desktop)
    {
        ArgumentNullException.ThrowIfNull(desktop);
        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        TrayIcon.SetIcons(this, [CreateTrayIcon()]);
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ConfigureDesktop(desktop);
            ShutdownAction ??= () => desktop.Shutdown();
            if (desktop.Args?.Contains(AgentInstallationBase.UninstallArgument, StringComparer.Ordinal) == true)
            {
                RunUninstall(desktop); // "Uygulamalar" → Kaldır (spec 0023 Req 11).
                base.OnFrameworkInitializationCompleted();
                return;
            }

            if (Composition is null && SelfInstall())
            {
                ShutdownWhenStarted(ShutdownAction);
                base.OnFrameworkInitializationCompleted();
                return;
            }

            Composition ??= AgentComposition.CreateDefault();
            var window = new MainWindow();
            window.Attach(Composition);
            AttachTrayStatus(Composition);
            desktop.MainWindow = window;

            // İlk kurulum (spec 0009 Req 2, 19–20): bileşenler eksik ya da onarılmalıysa önce kurulum ekranı; değilse 0001'deki gibi.
            // Bütünlük denetimi kısa (yalnızca kurulu exe'lerin hash'i); motor ConfigureAwait(false) kullanır, UI iş parçacığı kilitlenmez.
            if (Composition.Setup is { } setup && setup.InspectAsync(CancellationToken.None).GetAwaiter().GetResult())
            {
                ShowSetup(setup, Composition.Reports);
            }
            else
            {
                window.Show();
                if (Composition.Setup is { } ready)
                {
                    // Üretimin istediği bileşen sonradan kurulursa (spec 0027) ilerleme tray ipucunda görünür.
                    _setup = ready;
                    ready.Changed += (_, _) => Dispatcher.UIThread.Post(UpdateTrayMarker);
                }
            }
            var notifier = Composition.Notifier ?? new WindowsBalloonNotifier(() => window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero);
            if (Composition.Updater is { } updater)
            {
                // Güncelleme (spec 0023 Req 9): onay bekleyen yeni sürüm 60 sn içinde bağlanmalı; yarım kalmışsa önceki sürüme dönülür.
                var startup = updater.StartupAsync(_updateStop.Token).GetAwaiter().GetResult();
                if (startup == UpdateStartup.Restarting)
                {
                    ShutdownWhenStarted(ShutdownAction);
                    base.OnFrameworkInitializationCompleted();
                    return;
                }

                AttachUpdater(updater, notifier, startup == UpdateStartup.RolledBack);
                _ = updater.RunScheduleAsync(_updateStop.Token); // Açılışta ve 6 saatte bir (Req 1).
            }

            if (Composition.Reports is { } reports)
            {
                AttachErrorReports(reports, notifier);
                if (desktop.Args?.Contains(TestErrorArgument, StringComparer.Ordinal) == true)
                {
                    reports.Raise(AgentText.TestErrorSummary);
                }
            }

            _ = Composition.Session is { } session
                ? session.StartAsync(CancellationToken.None)
                : Composition.Connection.StartAsync(CancellationToken.None);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Açılışta (OnFrameworkInitializationCompleted içinde) uygulamayı kapatır: kapatma, ana döngü başladıktan sonra çalışsın diye UI
    /// kuyruğuna konur. Ana döngüden önce <c>Shutdown</c> çağrılırsa Avalonia dağıtıcısı kapanır ve döngü
    /// "Dispatcher shut down" hatasıyla çöker (ilk açılış kendini kurduktan sonra, güncelleme yeniden başlatması, kaldırmanın geçici kopyası).
    /// </summary>
    /// <param name="shutdown">Kapatma eylemi.</param>
    internal static void ShutdownWhenStarted(Action shutdown)
    {
        ArgumentNullException.ThrowIfNull(shutdown);
        Dispatcher.UIThread.Post(shutdown);
    }

    /// <summary>"Siteye git": siteyi varsayılan tarayıcıda açar.</summary>
    internal void OpenSite()
    {
        if (Composition is not null)
        {
            if (!Composition.Browser.Open(Composition.Settings.SiteUrl))
            {
                _notifier?.Show(AgentText.AppTitle, string.Format(CultureInfo.CurrentCulture, AgentText.BrowserOpenFailed, Composition.Settings.SiteUrl.AbsoluteUri)); // B-153
            }
        }
    }

    /// <summary>Kurulum ekranını açar ve tray ipucunu kurulum ilerlemesine bağlar (spec 0009 Req 16).</summary>
    /// <param name="setup">Kurulum motoru (önceden denetlenmiş).</param>
    /// <param name="reports">Hata bildirimi merkezi.</param>
    /// <returns>Pencere.</returns>
    internal SetupWindow ShowSetup(ISetupCoordinator setup, IErrorReportCenter? reports)
    {
        _setup = setup ?? throw new ArgumentNullException(nameof(setup));
        _setupStop = new CancellationTokenSource();
        var window = new SetupWindow();
        window.Attach(setup, reports, _setupStop.Token);
        window.Completed += (_, _) => CompleteSetup();
        setup.Changed += (_, _) => Dispatcher.UIThread.Post(UpdateTrayMarker);
        SetupWindow = window;
        window.Show();
        return window;
    }

    /// <summary>
    /// Kurulum tamamlandı · Hazır (spec 0009 Req 18): kurulum ekranı kapanır; ajan eşleşmemişse eşleştirme adımı (0012, durum
    /// penceresindeki kod) açılır, eşleşmişse ajan tray'de kalır.
    /// </summary>
    /// <returns>Eşleştirme adımı açıldıysa <c>true</c>.</returns>
    internal bool CompleteSetup()
    {
        SetupWindow?.CloseForGood();
        SetupWindow = null;
        UpdateTrayMarker();
        if (!NeedsPairing(Composition))
        {
            return false;
        }

        ShowMainWindow();
        return true;
    }

    /// <summary>Kurulum sonrası eşleştirme adımı gerekir mi (oturum akışı var ve eşleşmemiş).</summary>
    /// <param name="composition">Bileşim.</param>
    /// <returns>Gerekir mi.</returns>
    internal static bool NeedsPairing(AgentComposition? composition) =>
        composition?.Session is { } session && session.Current.State != AgentSessionState.Paired;

    /// <summary>"Çıkış": bağlantıyı kapatır ve uygulamayı sonlandırır.</summary>
    /// <returns>Görev.</returns>
    internal async Task ExitAsync()
    {
        try
        {
            await _updateStop.CancelAsync().ConfigureAwait(true);

            // Kurulum sürüyorsa güvenli durdur (spec 0009 Req 16): yarım bileşenin dosyaları silinir, tamamlananlar korunur.
            if (_setupStop is not null && SetupWindow?.Running is { } running)
            {
                await _setupStop.CancelAsync().ConfigureAwait(true);
                await running.ConfigureAwait(true);
            }

            if (Composition is not null)
            {
                if (Composition.Session is { } session)
                {
                    await session.StopAsync(CancellationToken.None).ConfigureAwait(true);
                }

                await Composition.Connection.StopAsync(CancellationToken.None).ConfigureAwait(true);
            }
        }
        finally
        {
            // Bağlantı kapatılamasa bile uygulama kapanmalı (review round 2, I-1).
            ShutdownAction?.Invoke();
        }
    }

    /// <summary>Tray menüsünün en üstündeki salt-okunur başlık: uygulama adı ve bağlantı durumu (spec 0009 Req 21a).</summary>
    /// <param name="state">Bağlantı durumu.</param>
    internal void ShowTrayStatus(ConnectionState state)
    {
        if (TrayStatusItem is not null)
        {
            TrayStatusItem.Header = string.Format(CultureInfo.CurrentCulture, AgentText.TrayStatusFormat, MainWindow.StateLabel(state));
        }
    }

    /// <summary>
    /// Tray menüsünün en üstündeki tıklanamayan satır: oturum durumu (spec 0012 Req 15) — "Eşleşmedi" · "Eşleşti" · "Oturum
    /// kapandı" · "Sunucuya ulaşılamıyor"; biçim 0009 Req 21a'daki başlıkla aynı ("ANEW Konsept · …").
    /// </summary>
    /// <param name="state">Oturum durumu.</param>
    internal void ShowTraySession(AgentSessionState state)
    {
        if (TrayStatusItem is not null)
        {
            TrayStatusItem.Header = string.Format(CultureInfo.CurrentCulture, AgentText.TrayStatusFormat, MainWindow.TrayLabel(state));
        }
    }

    /// <summary>
    /// Tray menüsü başlığını bileşime bağlar: oturum akışı varsa oturum durumu (plan 0012 Risk 4), yoksa canlı bağlantı durumu.
    /// </summary>
    /// <param name="composition">Ajan bileşimi.</param>
    internal void AttachTrayStatus(AgentComposition composition)
    {
        ArgumentNullException.ThrowIfNull(composition);
        if (composition.Session is { } session)
        {
            ShowTraySession(session.Current.State);
            session.Changed += (_, snapshot) => Dispatcher.UIThread.Post(() => ShowTraySession(snapshot.State));
            return;
        }

        ShowTrayStatus(composition.Connection.State);
        composition.Connection.StateChanged += (_, state) => Dispatcher.UIThread.Post(() => ShowTrayStatus(state));
    }

    /// <summary>Hata uyarılarını, bekleyen bildirim sayısını ve sonradan gelen bilgileri arayüze bağlar (spec 0022 Req 4, 7–8).</summary>
    /// <param name="reports">Hata bildirimi merkezi.</param>
    /// <param name="notifier">Windows bildirimi.</param>
    internal void AttachErrorReports(IErrorReportCenter reports, IDesktopNotifier notifier)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        reports.AlertRaised += (_, report) => Dispatcher.UIThread.Post(() => ShowAlert(report));
        reports.PendingChanged += (_, count) => Dispatcher.UIThread.Post(() => ShowPending(count));
        reports.Notice += (_, notice) => Dispatcher.UIThread.Post(() => ShowNotice(notice));
        ShowPending(reports.PendingCount);
    }

    /// <summary>
    /// Güncelleyiciyi arayüze bağlar (spec 0023): zamanlanmış denetim yeni sürüm bulunca tray bildirimi + menü satırı; sürüm uyumsuzluğunda
    /// güncelleme ekranı açılır; yeni/önceki sürüm başlatılınca bu süreç kapanır; geri dönüldüyse bildirim.
    /// </summary>
    /// <param name="updater">Güncelleyici.</param>
    /// <param name="notifier">Windows bildirimi.</param>
    /// <param name="rolledBack">Önceki sürüme dönülerek açıldı mı.</param>
    internal void AttachUpdater(IAgentUpdater updater, IDesktopNotifier notifier, bool rolledBack)
    {
        _updater = updater ?? throw new ArgumentNullException(nameof(updater));
        _updateNotifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        updater.AvailableFound += (_, manifest) => Dispatcher.UIThread.Post(() => ShowAvailable(manifest));
        updater.RestartRequested += (_, _) => Dispatcher.UIThread.Post(() => _ = ExitAsync());
        if (Composition?.Connection is { } connection)
        {
            connection.UpdateRequiredReceived += (_, serverVersion) => Dispatcher.UIThread.Post(() => _ = RequireUpdateAsync(serverVersion));
        }

        if (rolledBack)
        {
            notifier.Show(AgentText.AppTitle, AgentText.UpdateRolledBack);
        }
    }

    /// <summary>
    /// "Güncellemeleri denetle" (Req 1): hemen denetler; yeni sürüm varsa güncelleme ekranı açılır (başlatma yine kullanıcıda), yoksa
    /// "Ajan güncel (sürüm X)", doğrulanamazsa "Güncelleme doğrulanamadı, mevcut sürümle devam ediliyor".
    /// </summary>
    /// <returns>Sonuç.</returns>
    internal async Task<UpdateCheckOutcome> CheckUpdatesAsync()
    {
        if (_updater is null)
        {
            Notify(AgentText.UpdateDisabled);
            return UpdateCheckOutcome.Disabled;
        }

        var outcome = await _updater.CheckAsync(_updateStop.Token).ConfigureAwait(true);
        switch (outcome)
        {
            case UpdateCheckOutcome.Available when _updater.Available is { } manifest:
                ShowAvailable(manifest);
                ShowUpdateWindow();
                break;
            case UpdateCheckOutcome.UpToDate:
                Notify(string.Format(CultureInfo.CurrentCulture, AgentText.UpdateUpToDate, _updater.CurrentVersion));
                break;
            case UpdateCheckOutcome.VerificationFailed:
                Notify(AgentText.UpdateVerificationFailed);
                break;
            case UpdateCheckOutcome.Unavailable:
                Notify(AgentText.UpdateUnavailable);
                break;
            default:
                Notify(AgentText.UpdateDisabled);
                break;
        }

        return outcome;
    }

    /// <summary>Yeni sürüm hazır: tray bildirimi "Yeni sürüm hazır: X" ve menüde "Şimdi güncelle" satırı (Req 2).</summary>
    /// <param name="manifest">Liste.</param>
    internal void ShowAvailable(UpdateManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (TrayUpdateItem is not null)
        {
            TrayUpdateItem.Header = string.Format(CultureInfo.CurrentCulture, AgentText.TrayUpdateAvailable, manifest.Version);
            TrayUpdateItem.IsVisible = true;
            TrayUpdateItem.IsEnabled = true;
        }

        Notify(string.Format(CultureInfo.CurrentCulture, AgentText.UpdateAvailable, manifest.Version));
    }

    /// <summary>Güncelleme ekranını açar (tek pencere).</summary>
    /// <returns>Pencere; güncelleyici yoksa <c>null</c>.</returns>
    internal UpdateWindow? ShowUpdateWindow()
    {
        if (_updater is null)
        {
            return null;
        }

        if (UpdateWindow is null)
        {
            UpdateWindow = new UpdateWindow();
            UpdateWindow.Attach(_updater, _reports, _updateStop.Token);
        }

        UpdateWindow.Show();
        UpdateWindow.Activate();
        return UpdateWindow;
    }

    /// <summary>Hata uyarısını gösterir: pencere öne gelir, Windows bildirimi, tray işareti.</summary>
    /// <param name="report">Rapor.</param>
    /// <returns>Açılan pencere.</returns>
    internal ErrorAlertWindow ShowAlert(AgentErrorReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var window = new ErrorAlertWindow();
        window.Attach(_reports ?? throw new InvalidOperationException("Hata bildirimi bağlanmadı."), report);
        window.Closed += (_, _) =>
        {
            if (AlertWindow == window)
            {
                AlertWindow = null;
            }

            UpdateTrayMarker();
        };
        AlertWindow = window;
        window.Topmost = true;
        window.Show();
        window.Activate();
        window.Topmost = false;
        _notifier?.Show(AgentText.NotifyAlertTitle, report.Summary);
        UpdateTrayMarker();
        return window;
    }

    /// <summary>Bekleyen bildirim sayısını gösterir.</summary>
    /// <param name="count">Sayı.</param>
    internal void ShowPending(int count)
    {
        _pending = count;
        UpdateTrayMarker();
    }

    // İlk açılış (B-77): siteden indirilen exe kurulum yerine kopyalanır, HKCU kayıtları ve kısayol yazılır, kurulu kopya başlatılır.
    private static bool SelfInstall()
    {
        var exe = Environment.ProcessPath;
        if (!AgentComposition.IsReleaseExecutable(exe))
        {
            return false;
        }

        var installation = AgentComposition.CreateInstallation();
        try
        {
            if (installation.EnsureInstalled(exe!, new Infrastructure.Concrete.AssemblyAgentInfoProvider(typeof(App).Assembly).Version))
            {
                installation.LaunchInstalled(string.Empty);
                return true;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Kurulu kopya çalışıyor ya da yazılamıyor: bu exe olduğu yerden çalışmaya devam eder.
            Serilog.Log.Warning("Ajan kurulum yerine kopyalanamadı: {ExceptionType}", ex.GetType().Name);
        }

        return false;
    }

    // Sürüm uyumsuz (Req 4): ajan komut almaz; güncelleme ekranı açılır, yeni sürüm yoksa yalnızca bilgi verilir.
    private async Task RequireUpdateAsync(string serverVersion)
    {
        Notify(string.Format(CultureInfo.CurrentCulture, AgentText.UpdateRequiredNotice, serverVersion));
        if (_updater is not null
            && (_updater.Available is not null || await _updater.CheckAsync(_updateStop.Token).ConfigureAwait(true) == UpdateCheckOutcome.Available))
        {
            ShowUpdateWindow();
        }
    }

    private void Notify(string message) => _updateNotifier?.Show(AgentText.AppTitle, message);

    // "Uygulamalar" → Kaldır (Req 11): kurulu exe kendini silemeyeceği için geçici kopyadan çalışır; soru → kaldırma → kopya gecikmeli silinir.
    private void RunUninstall(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_trayIcon is not null)
        {
            _trayIcon.IsVisible = false;
        }

        var installation = AgentComposition.CreateInstallation();
        var host = new Infrastructure.Update.WindowsInstallHost();
        var exe = Environment.ProcessPath ?? string.Empty;
        if (exe.StartsWith(installation.InstallRoot, StringComparison.OrdinalIgnoreCase))
        {
            var copy = Path.Combine(Path.GetTempPath(), "anewconcept-kaldir-" + Guid.NewGuid().ToString("N") + ".exe");
            File.Copy(exe, copy);
            host.Launch(copy, AgentInstallationBase.UninstallArgument);
            ShutdownWhenStarted(() => desktop.Shutdown());
            return;
        }

        var videoFolder = new Infrastructure.Setup.FileSetupStateStore(installation.InstallRoot).Load().VideoFolder
            ?? Infrastructure.Setup.AgentPaths.DefaultVideoFolder;
        var window = new UninstallWindow();
        window.Attach(installation, videoFolder);
        window.Closed += (_, _) =>
        {
            if (exe.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
            {
                host.DeleteLater(exe);
            }

            desktop.Shutdown();
        };
        desktop.MainWindow = window;
        window.Show();
    }

    private void ShowNotice(ReportNotice notice) => _notifier?.Show(AgentText.AppTitle, notice switch
    {
        ReportNotice.OtherAccount => AgentText.AlertOtherAccount,
        ReportNotice.RateLimited => AgentText.AlertRateLimited,
        _ => AgentText.NotifyPendingSent,
    });

    private void UpdateTrayMarker()
    {
        TrayMarkerShown = AlertWindow is not null || _pending > 0;
        if (_trayIcon is not null)
        {
            _trayIcon.Icon = new WindowIcon(AssetLoader.Open(new Uri(TrayMarkerShown ? TrayAlertIconUri : TrayIconUri)));
            _trayIcon.ToolTipText = _setup?.IsRunning == true
                ? string.Format(CultureInfo.CurrentCulture, AgentText.SetupTrayProgress, _setup.Percent)
                : TrayMarkerShown ? AgentText.TrayAlertTooltip : AgentText.AppTitle;
        }

        if (TrayPendingItem is not null)
        {
            TrayPendingItem.Header = string.Format(CultureInfo.CurrentCulture, AgentText.TrayPendingReports, _pending);
            TrayPendingItem.IsVisible = TrayMarkerShown;
            TrayPendingItem.IsEnabled = TrayMarkerShown;
        }
    }

    private void ShowPendingAlert()
    {
        if (AlertWindow is { } alert)
        {
            alert.Show();
            alert.Activate();
            return;
        }

        ShowMainWindow();
    }

    private TrayIcon CreateTrayIcon()
    {
        TrayMenu = new NativeMenu();
        TrayStatusItem = new NativeMenuItem
        {
            IsEnabled = false,
            Icon = new Avalonia.Media.Imaging.Bitmap(AssetLoader.Open(new Uri("avares://Konsept.Agent/Assets/tray-icon.png"))),
        };
        ShowTrayStatus(ConnectionState.Disconnected);
        TrayMenu.Items.Add(TrayStatusItem);
        // Komut atanınca IsEnabled komuttan dolar; bu yüzden komut önce, pasiflik sonra (bekleyen yokken menüde etkin satır olmasın).
        TrayPendingItem = new NativeMenuItem { Command = new DelegateCommand(ShowPendingAlert), IsVisible = false, IsEnabled = false };
        TrayMenu.Items.Add(TrayPendingItem);
        TrayMenu.Items.Add(new NativeMenuItemSeparator());
        TrayMenu.Items.Add(new NativeMenuItem(AgentText.OpenSite) { Command = new DelegateCommand(OpenSite) });
        TrayCheckUpdatesItem = new NativeMenuItem(AgentText.TrayCheckUpdates) { Command = new DelegateCommand(() => _ = CheckUpdatesAsync()) };
        TrayMenu.Items.Add(TrayCheckUpdatesItem);
        TrayUpdateItem = new NativeMenuItem { Command = new DelegateCommand(() => ShowUpdateWindow()), IsVisible = false, IsEnabled = false };
        TrayMenu.Items.Add(TrayUpdateItem);
        TrayMenu.Items.Add(new NativeMenuItemSeparator());
        TrayMenu.Items.Add(new NativeMenuItem(AgentText.Exit) { Command = new DelegateCommand(() => _ = ExitAsync()) });

        var icon = new TrayIcon
        {
            ToolTipText = AgentText.AppTitle,
            Menu = TrayMenu,
            Icon = new WindowIcon(AssetLoader.Open(new Uri(TrayIconUri))),
        };
        icon.Clicked += (_, _) => ShowMainWindow();
        _trayIcon = icon;
        return icon;
    }

    private void ShowMainWindow()
    {
        if (SetupWindow is { } setupWindow)
        {
            setupWindow.Show();
            setupWindow.Activate();
            return;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            window.Show();
            window.Activate();
        }
    }
}
