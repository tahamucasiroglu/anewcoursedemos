using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using NSubstitute;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Ajan penceresi ve tray satırında eşleştirme (spec 0012 AC-1, AC-2, AC-14, AC-15 ajan tarafı) — pencere açmadan, headless.
/// </summary>
public sealed class PairingWindowTests
{
    private static readonly Uri Site = new("http://192.168.1.10:8000/");

    private static App CurrentApp => (App)Avalonia.Application.Current!;

    /// <summary>
    /// Kod büyük ve seçilip kopyalanabilir bir metinde gösterilir, geri sayım görünür; süre dolunca kod kalkar ve "Yeni kod al"
    /// görünür.
    /// </summary>
    [AvaloniaFact]
    public void Window_CodeSelectable_CountdownShown()
    {
        var session = Session(new AgentSessionSnapshot(AgentSessionState.Unpaired, "ABCD-2345", DateTimeOffset.UtcNow.AddMinutes(9).AddSeconds(30)));
        var window = new MainWindow();

        window.Attach(Composition(Substitute.For<IBrowserLauncher>(), session));

        window.CodeDisplay.ShouldBe("ABCD-2345");
        window.CodeControl.ShouldBeOfType<SelectableTextBlock>();
        window.CountdownDisplay.ShouldStartWith("Kod 09:");
        window.SessionDisplay.ShouldStartWith("Eşleşmedi");
        window.NewCodeVisible.ShouldBeFalse();

        window.ShowSession(new AgentSessionSnapshot(AgentSessionState.CodeExpired));

        window.CodeDisplay.ShouldBeEmpty();
        window.SessionDisplay.ShouldBe("Kodun süresi doldu. Yeni kod alın.");
        window.NewCodeVisible.ShouldBeTrue();
    }

    /// <summary>Penceredeki "Siteye git" web'deki "Ajanı eşleştir" sayfasını açar; adres kodu içermez.</summary>
    [AvaloniaFact]
    public void Window_OpenPairingPage_AddressHasNoCode()
    {
        var browser = Substitute.For<IBrowserLauncher>();
        var window = new MainWindow();
        window.Attach(Composition(browser, Session(new AgentSessionSnapshot(AgentSessionState.Unpaired, "ABCD-2345", DateTimeOffset.UtcNow.AddMinutes(10)))));

        window.OpenPairingPage();

        browser.Received(1).Open(Arg.Is<Uri>(u => u.AbsoluteUri == "http://192.168.1.10:8000/ajan-eslestir"));
        browser.DidNotReceive().Open(Arg.Is<Uri>(u => u.AbsoluteUri.Contains("ABCD", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>B-153: tarayıcı açılamazsa pencere düşmez, adresi elle açılsın diye ipucunda gösterir.</summary>
    [AvaloniaFact]
    public void Window_OpenPairingPage_BrowserFails_ShowsAddress()
    {
        var browser = Substitute.For<IBrowserLauncher>();
        browser.Open(Arg.Any<Uri>()).Returns(false);
        var window = new MainWindow();
        window.Attach(Composition(browser, Session(new AgentSessionSnapshot(AgentSessionState.Unpaired, "ABCD-2345", DateTimeOffset.UtcNow.AddMinutes(10)))));

        Should.NotThrow(window.OpenPairingPage);

        window.HintDisplay.ShouldBe("Tarayıcı açılamadı. Bu adresi tarayıcınızda açın: http://192.168.1.10:8000/ajan-eslestir");
    }

    /// <summary>
    /// Tray menüsünün en üstündeki tıklanamayan satır oturum durumunu izler: "Eşleşmedi" → "Eşleşti" → "Oturum kapandı" →
    /// "Sunucuya ulaşılamıyor"; "Siteye git" ve "Çıkış" değişmez.
    /// </summary>
    [AvaloniaFact]
    public void TrayStatus_FollowsSessionState()
    {
        var session = Session(new AgentSessionSnapshot(AgentSessionState.Unpaired, "ABCD-2345", DateTimeOffset.UtcNow.AddMinutes(10)));
        var status = CurrentApp.TrayStatusItem!;
        try
        {
            CurrentApp.AttachTrayStatus(Composition(Substitute.For<IBrowserLauncher>(), session));
            CurrentApp.TrayMenu!.Items[0].ShouldBeSameAs(status);
            status.IsEnabled.ShouldBeFalse();
            status.Header.ShouldBe("ANEW Konsept · Eşleşmedi");

            var seen = new List<string?>();
            foreach (var state in new[] { AgentSessionState.Paired, AgentSessionState.SessionClosed, AgentSessionState.Unreachable })
            {
                session.Changed += Raise.Event<EventHandler<AgentSessionSnapshot>>(session, new AgentSessionSnapshot(state));
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                seen.Add(status.Header);
            }

            seen.ShouldBe(["ANEW Konsept · Eşleşti", "ANEW Konsept · Oturum kapandı", "ANEW Konsept · Sunucuya ulaşılamıyor"]);
            CurrentApp.TrayMenu.Items.OfType<NativeMenuItem>()
                .Where(i => i is not NativeMenuItemSeparator && i.IsEnabled)
                .Select(i => i.Header)
                .ShouldBe(["Siteye git", "Güncellemeleri denetle", "Çıkış"]); // spec 0023 Req 12 (plan 0023 A-2).
        }
        finally
        {
            CurrentApp.ShowTrayStatus(ConnectionState.Disconnected);
        }
    }

    private static IAgentSessionCoordinator Session(AgentSessionSnapshot current)
    {
        var session = Substitute.For<IAgentSessionCoordinator>();
        session.Current.Returns(current);
        return session;
    }

    private static AgentComposition Composition(IBrowserLauncher browser, IAgentSessionCoordinator session)
    {
        var info = Substitute.For<IAgentInfoProvider>();
        info.MachineName.Returns("TAHA-PC");
        info.Version.Returns("0.5.0");
        var connection = Substitute.For<IServerConnection>();
        connection.State.Returns(ConnectionState.Disconnected);
        return new AgentComposition(new AgentSettings(new Uri("http://192.168.1.10:8001/"), Site, TimeSpan.FromSeconds(1)), info, connection, browser, Session: session);
    }
}
