using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Tray menüsü ve pencere davranışı (spec 0001 AC-5) — pencere açmadan, headless.
/// </summary>
public sealed class TrayAndWindowTests
{
    private static App CurrentApp => (App)Avalonia.Application.Current!;

    /// <summary>Tray menüsünde "Siteye git" ve "Çıkış" vardır.</summary>
    [AvaloniaFact]
    public void Tray_Menu_HasOpenSiteAndExit()
    {
        var icons = TrayIcon.GetIcons(CurrentApp);

        icons.ShouldNotBeNull();
        icons.Count.ShouldBe(1);
        var headers = CurrentApp.TrayMenu!.Items.OfType<NativeMenuItem>()
            .Where(i => i is not NativeMenuItemSeparator)
            .Select(i => i.Header)
            .ToList();
        headers.ShouldBe(["Siteye git", "Çıkış"]);
        icons[0].ToolTipText.ShouldBe("ANEW Konsept Ajan");
    }

    /// <summary>"Siteye git" ayarlardaki site adresini varsayılan tarayıcıda açar.</summary>
    [AvaloniaFact]
    public void TrayMenu_OpenSite_OpensSiteUrlInBrowser()
    {
        var browser = Substitute.For<IBrowserLauncher>();
        var site = new Uri("http://192.168.1.10:8000/");
        CurrentApp.Composition = Composition(site, browser, Substitute.For<IServerConnection>());
        try
        {
            var openSite = CurrentApp.TrayMenu!.Items.OfType<NativeMenuItem>().First();

            openSite.Command!.Execute(null);

            browser.Received(1).Open(site);
        }
        finally
        {
            CurrentApp.Composition = null;
        }
    }

    /// <summary>Pencere kapatılınca uygulama kapanmaz; pencere yalnızca gizlenir.</summary>
    [AvaloniaFact]
    public void MainWindow_Close_DoesNotShutdown()
    {
        var window = new MainWindow();
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();

        window.Close();

        window.IsVisible.ShouldBeFalse();
        closed.ShouldBeFalse();
    }

    /// <summary>Masaüstü yaşam döngüsü "yalnızca açıkça kapat" moduna alınır.</summary>
    [Fact]
    public void App_ConfigureDesktop_SetsExplicitShutdown()
    {
        var desktop = Substitute.For<IClassicDesktopStyleApplicationLifetime>();

        App.ConfigureDesktop(desktop);

        desktop.Received().ShutdownMode = ShutdownMode.OnExplicitShutdown;
    }

    /// <summary>Pencere bağlantı durumunu canlı gösterir.</summary>
    [AvaloniaFact]
    public void MainWindow_ConnectionStateChanges_AreShown()
    {
        var connection = Substitute.For<IServerConnection>();
        connection.State.Returns(ConnectionState.Disconnected);
        var window = new MainWindow();
        window.Attach(Composition(new Uri("http://localhost:8000/"), Substitute.For<IBrowserLauncher>(), connection));
        window.StateDisplay.ShouldBe("Sunucuya bağlı değil");

        connection.StateChanged += Raise.Event<EventHandler<ConnectionState>>(connection, ConnectionState.Connected);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        window.StateDisplay.ShouldBe("Sunucuya bağlı");
    }

    private static AgentComposition Composition(Uri site, IBrowserLauncher browser, IServerConnection connection)
    {
        var info = Substitute.For<IAgentInfoProvider>();
        info.MachineName.Returns("TAHA-PC");
        info.Version.Returns("0.1.0");
        return new AgentComposition(new AgentSettings(new Uri("http://localhost:8001/"), site, TimeSpan.FromSeconds(1)), info, connection, browser);
    }
}
