using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Resources;

namespace Konsept.Agent;

/// <summary>
/// Ajan uygulaması: tray ikonu ve menüsü ("Siteye git", "Çıkış"), durum penceresi ve sunucu bağlantısı
/// (spec 0001 Req 6–7, AC-5, AC-6). Uygulama yalnızca "Çıkış" ile kapanır.
/// </summary>
public sealed partial class App : Avalonia.Application
{
    /// <summary>Gets or sets bileşim (testte sahte bileşenlerle değiştirilebilir).</summary>
    internal AgentComposition? Composition { get; set; }

    /// <summary>Gets or sets uygulamayı sonlandıran eylem (masaüstünde lifetime.Shutdown; testte sahte).</summary>
    internal Action? ShutdownAction { get; set; }

    /// <summary>Gets tray menüsü.</summary>
    internal NativeMenu? TrayMenu { get; private set; }

    /// <summary>Gets tray menüsünün durum başlığı (tıklanamaz).</summary>
    internal NativeMenuItem? TrayStatusItem { get; private set; }

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
        TrayMenu.Items.Add(new NativeMenuItemSeparator());
        TrayMenu.Items.Add(new NativeMenuItem(AgentText.OpenSite) { Command = new DelegateCommand(OpenSite) });
        TrayMenu.Items.Add(new NativeMenuItemSeparator());
        TrayMenu.Items.Add(new NativeMenuItem(AgentText.Exit) { Command = new DelegateCommand(() => _ = ExitAsync()) });

        var icon = new TrayIcon
        {
            ToolTipText = AgentText.AppTitle,
            Menu = TrayMenu,
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Konsept.Agent/Assets/tray-icon.png"))),
        };
        icon.Clicked += (_, _) => ShowMainWindow();
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
