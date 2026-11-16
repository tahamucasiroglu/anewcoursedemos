using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Konsept.Agent.Abstract;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Concrete;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Resources;

namespace Konsept.Agent;

/// <summary>
/// Ajan uygulaması: tray ikonu ve menüsü ("Siteye git", "Çıkış"), durum penceresi ve sunucu bağlantısı
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
            Composition ??= AgentComposition.CreateDefault();
            var window = new MainWindow();
            window.Attach(Composition);
            AttachTrayStatus(Composition);
            desktop.MainWindow = window;
            window.Show();
            if (Composition.Reports is { } reports)
            {
                AttachErrorReports(reports, Composition.Notifier ?? new WindowsBalloonNotifier(() => window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero));
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

    /// <summary>"Siteye git": siteyi varsayılan tarayıcıda açar.</summary>
    internal void OpenSite()
    {
        if (Composition is not null)
        {
            Composition.Browser.Open(Composition.Settings.SiteUrl);
        }
    }

    /// <summary>"Çıkış": bağlantıyı kapatır ve uygulamayı sonlandırır.</summary>
    /// <returns>Görev.</returns>
    internal async Task ExitAsync()
    {
        try
        {
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
            _trayIcon.ToolTipText = TrayMarkerShown ? AgentText.TrayAlertTooltip : AgentText.AppTitle;
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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            window.Show();
            window.Activate();
        }
    }
}
