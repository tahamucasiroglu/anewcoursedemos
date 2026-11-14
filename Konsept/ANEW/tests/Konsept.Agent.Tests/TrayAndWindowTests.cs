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
            .Where(i => i is not NativeMenuItemSeparator && i.IsEnabled)
            .Select(i => i.Header)
            .ToList();
        headers.ShouldBe(["Siteye git", "Çıkış"]);
        icons[0].ToolTipText.ShouldBe("ANEW Konsept Ajan");
    }

    /// <summary>Tray menüsü tıklanamaz bir durum başlığıyla açılır ve bağlantı durumunu canlı gösterir (spec 0009 AC-26).</summary>
    [AvaloniaFact]
    public void TrayMenu_StatusHeader_ShowsLiveConnectionState()
    {
        var connection = Substitute.For<IServerConnection>();
        connection.State.Returns(ConnectionState.Disconnected);
        var status = CurrentApp.TrayStatusItem!;
        CurrentApp.TrayMenu!.Items[0].ShouldBeSameAs(status);
        status.IsEnabled.ShouldBeFalse();

        CurrentApp.AttachTrayStatus(Composition(new Uri("http://localhost:8000/"), Substitute.For<IBrowserLauncher>(), connection));
        status.Header.ShouldBe("ANEW Konsept · Sunucuya bağlı değil");

        connection.StateChanged += Raise.Event<EventHandler<ConnectionState>>(connection, ConnectionState.Connected);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();

        status.Header.ShouldBe("ANEW Konsept · Sunucuya bağlı");
        CurrentApp.ShowTrayStatus(ConnectionState.Disconnected);
    }

    /// <summary>Uygulama teması Windows'u izler ve tray menüsü site paletini kullanır (spec 0009 AC-26).</summary>
    [AvaloniaFact]
    public void App_Theme_FollowsSystemAndMenuUsesSitePalette()
    {
        CurrentApp.RequestedThemeVariant.ShouldBe(Avalonia.Styling.ThemeVariant.Default);

        CurrentApp.TryGetResource("KonseptPrimary", Avalonia.Styling.ThemeVariant.Light, out var light).ShouldBeTrue();
        CurrentApp.TryGetResource("KonseptPrimary", Avalonia.Styling.ThemeVariant.Dark, out var dark).ShouldBeTrue();
        light.ShouldBe(Avalonia.Media.Color.Parse("#B85A1A"));
        dark.ShouldBe(Avalonia.Media.Color.Parse("#E8772E"));
        CurrentApp.TryGetResource("MenuFlyoutPresenterBackground", Avalonia.Styling.ThemeVariant.Dark, out var menuBg).ShouldBeTrue();
        menuBg.ShouldBeOfType<Avalonia.Media.SolidColorBrush>();
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
            var openSite = CurrentApp.TrayMenu!.Items.OfType<NativeMenuItem>().Single(i => i.Header == "Siteye git");

            openSite.Command!.Execute(null);

            browser.Received(1).Open(site);
        }
        finally
        {
            CurrentApp.Composition = null;
        }
    }

    /// <summary>"Çıkış" önce sunucu bağlantısını kapatır, sonra uygulamayı sonlandırır (review L-3).</summary>
    /// <returns>Görev.</returns>
    [AvaloniaFact]
    public async Task TrayMenu_Exit_StopsConnectionThenShutsDown()
    {
        var connection = Substitute.For<IServerConnection>();
        var order = new List<string>();
        connection.StopAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            order.Add("stop");
            return Task.CompletedTask;
        });
        CurrentApp.Composition = Composition(new Uri("http://localhost:8000/"), Substitute.For<IBrowserLauncher>(), connection);
        CurrentApp.ShutdownAction = () => order.Add("shutdown");
        try
        {
            var exit = CurrentApp.TrayMenu!.Items.OfType<NativeMenuItem>().Last();
            exit.Header.ShouldBe("Çıkış");

            await CurrentApp.ExitAsync();

            order.ShouldBe(["stop", "shutdown"]);
        }
        finally
        {
            CurrentApp.Composition = null;
            CurrentApp.ShutdownAction = null;
        }
    }

    /// <summary>Bağlantı kapatılırken hata olsa bile "Çıkış" uygulamayı sonlandırır (review round 2, I-1).</summary>
    /// <returns>Görev.</returns>
    [AvaloniaFact]
    public async Task TrayMenu_Exit_StopFails_StillShutsDown()
    {
        var connection = Substitute.For<IServerConnection>();
        connection.StopAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("kapatılamadı")));
        CurrentApp.Composition = Composition(new Uri("http://localhost:8000/"), Substitute.For<IBrowserLauncher>(), connection);
        var shutdown = false;
        CurrentApp.ShutdownAction = () => shutdown = true;
        try
        {
            await Should.ThrowAsync<InvalidOperationException>(CurrentApp.ExitAsync);

            shutdown.ShouldBeTrue();
        }
        finally
        {
            CurrentApp.Composition = null;
            CurrentApp.ShutdownAction = null;
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
