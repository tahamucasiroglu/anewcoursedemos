using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Setup;
using NSubstitute;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// İlk kurulum ekranı (spec 0009 AC-2, AC-3, AC-5, AC-13, AC-14, AC-16, AC-18, AC-23) — headless, sahte indirici, gerçek motor.
/// </summary>
public sealed class SetupWindowTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("anewconcept-setupui-");
    private readonly FakeSetupHost _host = new();
    private readonly SetupCoordinator _setup;

    /// <summary>Initializes a new instance of the <see cref="SetupWindowTests"/> class.</summary>
    public SetupWindowTests()
    {
        var root = Path.Combine(_root.FullName, "anewconcept");
        _setup = new SetupCoordinator(
            [_host.Add("alpha"), _host.Add("beta")],
            _host,
            new FileSetupStateStore(root),
            root,
            Path.Combine(_root.FullName, "Videolar", "anewconcept"),
            null);
        _setup.InspectAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    private static App CurrentApp => (App)Avalonia.Application.Current!;

    /// <inheritdoc />
    public void Dispose() => _root.Delete(recursive: true);

    /// <summary>AC-2: bileşenler ad, amaç, lisans, indirme boyutu ve "Bekliyor" ile listelenir; çekirdekler zorunludur.</summary>
    [AvaloniaFact]
    public void SetupWindow_ListsComponentsWaitingAndLicense()
    {
        var window = Attach();

        window.RowStatus("alpha").ShouldBe("Bekliyor");
        window.RowStatus("beta").ShouldBe("Bekliyor");
        window.RowAccessibleName("alpha").ShouldBe("ALPHA: Bekliyor");
        window.RowButtons("alpha").ShouldBeEmpty();
        window.VideoFolderDisplay.ShouldBe(Path.Combine(_root.FullName, "Videolar", "anewconcept"));
        window.SetVideoFolder(_setup.InstallRoot).ShouldBeFalse();
        window.VideoErrorVisible.ShouldBeTrue();
        window.SetVideoFolder(Path.Combine(_root.FullName, "Başka")).ShouldBeTrue();
        window.VideoErrorVisible.ShouldBeFalse();
    }

    /// <summary>AC-3, AC-11: onay kutusu işaretlenmeden, internet yokken ya da disk yetmezken Başlat pasiftir ve sebep görünür.</summary>
    [AvaloniaFact]
    public void SetupWindow_StartDisabledUntilLicenseAccepted()
    {
        var window = Attach();

        window.StartEnabled.ShouldBeFalse();
        window.ReasonDisplay.ShouldBe("Başlatmak için lisans bilgisini okuyup onaylayın.");
        window.SpaceDisplay.ShouldStartWith("Gereken: ");
        window.SpaceDisplay.ShouldContain("GB, Boş: ");

        window.AcceptBox.IsChecked = true;
        window.StartEnabled.ShouldBeTrue();
        window.ReasonDisplay.ShouldBeEmpty();

        _host.Online = false;
        window.AcceptBox.IsChecked = false;
        window.AcceptBox.IsChecked = true;
        window.StartEnabled.ShouldBeFalse();
        window.ReasonDisplay.ShouldStartWith("İnternet bağlantısı yok");

        _host.Online = true;
        _host.Free = 1;
        window.AcceptBox.IsChecked = false;
        window.AcceptBox.IsChecked = true;
        window.StartEnabled.ShouldBeFalse();
        window.ReasonDisplay.ShouldBe("Diskte yeterli yer yok.");
    }

    /// <summary>AC-5: "Şu an … kuruluyor", ilerleme ve Türkçe akan satırlar; sonunda "Kurulum tamamlandı · Hazır".</summary>
    /// <returns>Görev.</returns>
    [AvaloniaFact]
    public async Task SetupWindow_ShowsCurrentComponentAndLines()
    {
        var window = Attach();
        var completed = 0;
        window.Completed += (_, _) => completed++;
        window.AcceptBox.IsChecked = true;
        _host.Hold = new TaskCompletionSource();

        var run = window.StartAsync();
        Dispatcher.UIThread.RunJobs();
        window.CurrentDisplay.ShouldBe("Şu an ALPHA kuruluyor");
        window.RowStatus("alpha").ShouldBe("İndiriliyor");
        window.StartEnabled.ShouldBeFalse();
        _host.Hold.SetResult();
        await run;
        Dispatcher.UIThread.RunJobs();

        window.Lines.ShouldBe(
        [
            "Kurulum başladı",
            "ALPHA indiriliyor", "ALPHA doğrulanıyor", "ALPHA kuruluyor", "ALPHA kuruldu",
            "BETA indiriliyor", "BETA doğrulanıyor", "BETA kuruluyor", "BETA kuruldu",
            "Hazır denetimi yapılıyor…", "Kurulum tamamlandı · Hazır",
        ]);
        window.RowStatus("beta").ShouldBe("Kuruldu");
        window.DoneDisplay.ShouldBe("Kurulum tamamlandı · Hazır");
        window.ProgressValue.ShouldBe(100);
        completed.ShouldBe(1);
    }

    /// <summary>
    /// AC-13, AC-14: hatalı satır kırmızı ve "Hata — sebep" yazısıyla; "Tekrar dene" ve "Hatayı bildir" yalnızca orada; bildir 0022 akışını
    /// bileşen adı ve adımla açar; Tekrar dene yalnızca o bileşeni kurar.
    /// </summary>
    /// <returns>Görev.</returns>
    [AvaloniaFact]
    public async Task SetupWindow_FailedRowRedTextRetryReport()
    {
        var reports = Substitute.For<IErrorReportCenter>();
        var window = Attach(reports);
        window.AcceptBox.IsChecked = true;
        var beta = _setup.Components.Single(c => c.Component.Id == "beta").Component;
        _host.Unreachable.Add(beta.Url);

        await window.StartAsync();
        Dispatcher.UIThread.RunJobs();

        window.RowStatus("beta").ShouldBe("Hata — Dosya indirilemedi");
        window.RowAccessibleName("beta").ShouldBe("BETA: Hata — Dosya indirilemedi");
        window.RowIsError("beta").ShouldBeTrue();
        window.RowIsError("alpha").ShouldBeFalse();
        window.RowButtons("beta").ShouldBe(["Tekrar dene", "Hatayı bildir"]);
        window.RowButtons("alpha").ShouldBeEmpty();
        window.Lines.ShouldContain("BETA indirilemedi, yeniden deneniyor (1/2)");
        window.Lines.ShouldContain("BETA: Hata — Dosya indirilemedi");

        window.ReportFailure("alpha");
        reports.DidNotReceiveWithAnyArgs().Raise(default!);
        window.ReportFailure("beta");
        reports.Received(1).Raise("Kurulum hatası: BETA — Dosya indirilemedi", "BETA", InstallStep.Download);

        _host.Unreachable.Clear();
        _host.Downloads.Clear();
        await window.RetryAsync("beta");
        Dispatcher.UIThread.RunJobs();
        _host.Downloads.ShouldBe([beta.Url]);
        window.RowStatus("beta").ShouldBe("Kuruldu");
        window.RowButtons("beta").ShouldBeEmpty();
    }

    /// <summary>AC-16: kurulum sürerken kapatma onay sorar; Hayır → açık kalır; Evet → gizlenir, kurulum sürer, tray ipucu ilerlemeyi gösterir.</summary>
    /// <returns>Görev.</returns>
    [AvaloniaFact]
    public async Task SetupWindow_CloseWhileRunning_AsksBackground()
    {
        _host.Hold = new TaskCompletionSource();
        var window = CurrentApp.ShowSetup(_setup, null);
        try
        {
            var background = 0;
            window.MovedToBackground += (_, _) => background++;
            window.AcceptBox.IsChecked = true;
            var run = window.StartAsync();
            Dispatcher.UIThread.RunJobs();
            CurrentApp.TrayToolTip.ShouldBe("Kuruluyor: %0");

            window.Close();
            window.IsVisible.ShouldBeTrue();
            window.ConfirmVisible.ShouldBeTrue();
            window.AnswerBackground(false);
            window.IsVisible.ShouldBeTrue();
            window.ConfirmVisible.ShouldBeFalse();

            window.Close();
            window.AnswerBackground(true);
            window.IsVisible.ShouldBeFalse();
            background.ShouldBe(1);
            _setup.IsRunning.ShouldBeTrue();

            _host.Hold.SetResult();
            await run;
            Dispatcher.UIThread.RunJobs();
            _setup.IsReady.ShouldBeTrue();
            CurrentApp.SetupWindow.ShouldBeNull();
            CurrentApp.TrayToolTip.ShouldNotStartWith("Kuruluyor");
        }
        finally
        {
            CurrentApp.CompleteSetup();
        }
    }

    /// <summary>AC-18: kurulum bitince eşleşmemiş ajan eşleştirme adımına geçer; eşleşmişse tray'de kalır.</summary>
    [AvaloniaFact]
    public void SetupCompleted_UnpairedShowsMain_PairedStaysTray()
    {
        var session = Substitute.For<IAgentSessionCoordinator>();
        session.Current.Returns(new AgentSessionSnapshot(AgentSessionState.Paired, null, null));
        App.NeedsPairing(Composition(session)).ShouldBeFalse();

        session.Current.Returns(new AgentSessionSnapshot(AgentSessionState.Unpaired, "ABCD-1234", null));
        App.NeedsPairing(Composition(session)).ShouldBeTrue();
        App.NeedsPairing(null).ShouldBeFalse();
    }

    /// <summary>AC-23: tüm kontrollerin Türkçe erişilebilir adı var; hata rengi açık ve koyu temada paletten gelir.</summary>
    [AvaloniaFact]
    public void SetupWindow_ControlsHaveAccessibleNames()
    {
        var window = Attach();

        window.AccessibleNames.ShouldBe(["Video klasörü", "Değiştir", "Okudum ve kabul ediyorum", "Kurulumu başlat", "Kurulum ilerlemesi", "Yapılan işler", "Evet", "Hayır"]);
        window.Title.ShouldBe("ANEW Konsept · Kurulum");
        CurrentApp.TryGetResource("KonseptError", Avalonia.Styling.ThemeVariant.Light, out var light).ShouldBeTrue();
        CurrentApp.TryGetResource("KonseptError", Avalonia.Styling.ThemeVariant.Dark, out var dark).ShouldBeTrue();
        light.ShouldNotBe(dark);
    }

    private static AgentComposition Composition(IAgentSessionCoordinator session) =>
        new(
            new AgentSettings(new Uri("http://localhost:8001/"), new Uri("http://localhost:8000/"), TimeSpan.FromSeconds(1)),
            Substitute.For<IAgentInfoProvider>(),
            Substitute.For<IServerConnection>(),
            Substitute.For<IBrowserLauncher>(),
            Session: session);

    private SetupWindow Attach(IErrorReportCenter? reports = null)
    {
        var window = new SetupWindow();
        window.Attach(_setup, reports, CancellationToken.None, () => Task.FromResult<string?>(null));
        return window;
    }
}
