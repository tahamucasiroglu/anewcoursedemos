using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
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
            desktop.MainWindow = window;
            window.Show();
            _ = Composition.Connection.StartAsync(CancellationToken.None);
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
        if (Composition is not null)
        {
            await Composition.Connection.StopAsync(CancellationToken.None).ConfigureAwait(true);
        }

        ShutdownAction?.Invoke();
    }

    private TrayIcon CreateTrayIcon()
    {
        TrayMenu = new NativeMenu();
        TrayMenu.Items.Add(new NativeMenuItem(AgentText.OpenSite) { Command = new DelegateCommand(OpenSite) });
        TrayMenu.Items.Add(new NativeMenuItemSeparator());
        TrayMenu.Items.Add(new NativeMenuItem(AgentText.Exit) { Command = new DelegateCommand(() => _ = ExitAsync()) });

        var icon = new TrayIcon
        {
            ToolTipText = AgentText.AppTitle,
            Menu = TrayMenu,
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Konsept.Agent/Assets/tray-placeholder.png"))),
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
