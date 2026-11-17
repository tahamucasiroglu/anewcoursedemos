using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Konsept.Agent.Abstract;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Setup;
using NSubstitute;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Güncelleme ve kaldırma arayüzü (spec 0023 AC-1, AC-2, AC-9, AC-11, AC-14): güncelleme ekranı başlığı, satırı ve durumları; onaysız
/// güncelleme yok; Hata satırında Tekrar dene / Hatayı bildir; tray "Güncellemeleri denetle"; kaldırma sorusunda varsayılan Hayır; kurulum
/// ekranının güncelleme kipi yalnızca değişen bileşenleri listeler.
/// </summary>
public sealed class UpdateWindowTests
{
    private static readonly UpdateManifest Manifest = new("0.6.0", "anewconcept-ajan-0.6.0-win-x64.exe", new string('a', 64), 10);
    private readonly IAgentUpdater _updater = Substitute.For<IAgentUpdater>();

    /// <summary>Initializes a new instance of the <see cref="UpdateWindowTests"/> class.</summary>
    public UpdateWindowTests()
    {
        _updater.CurrentVersion.Returns("0.5.0");
        _updater.Available.Returns(Manifest);
        _updater.Current.Returns(new UpdateEvent(UpdateStatus.Idle));
        _updater.ApplyAsync(Arg.Any<CancellationToken>()).Returns(true);
    }

    private static App CurrentApp => (App)Avalonia.Application.Current!;

    /// <summary>
    /// AC-1, AC-2: ekran "Şu an bunları güncelliyoruz" başlığıyla, yalnızca ajan satırıyla (kurulu → yeni sürüm) açılır; güncelleme yalnızca
    /// "Şimdi güncelle" ile başlar; olaylar satıra ve akan satırlara yansır, sonunda "Güncelleme tamamlandı".
    /// </summary>
    /// <returns>Görev.</returns>
    [AvaloniaFact]
    public async Task UpdateWindow_ShowsHeadingRowAndCompleted()
    {
        var window = Attach();

        window.Heading.ShouldBe("Şu an bunları güncelliyoruz");
        window.RowDetailText.ShouldBe("Kurulu sürüm 0.5.0 → yeni sürüm 0.6.0");
        window.RowStatusText.ShouldBe("Bekliyor");
        window.Message.ShouldBe("Yeni sürüm hazır: 0.6.0");
        window.StartEnabled.ShouldBeTrue();
        await _updater.DidNotReceive().ApplyAsync(Arg.Any<CancellationToken>());

        await window.StartAsync();
        await _updater.Received(1).ApplyAsync(Arg.Any<CancellationToken>());

        Emit(new UpdateEvent(UpdateStatus.Downloading, 40, Version: "0.6.0"));
        window.RowStatusText.ShouldBe("İndiriliyor %40");
        Emit(new UpdateEvent(UpdateStatus.Verifying, 100, Version: "0.6.0"));
        Emit(new UpdateEvent(UpdateStatus.Completed, 100, Version: "0.6.0"));
        window.Message.ShouldBe("Güncelleme tamamlandı");
        window.RowStatusText.ShouldBe("Güncellendi");
        window.Lines.ShouldBe(["Güncelleme başladı; yeni sürüm indiriliyor", "Doğrulanıyor", "Güncelleme tamamlandı"]);
        window.StartEnabled.ShouldBeFalse();
    }

    /// <summary>AC-3, AC-4: bekleme ve doğrulama başarısız metinleri.</summary>
    [AvaloniaFact]
    public void UpdateWindow_WaitingAndVerificationFailedMessages()
    {
        var window = Attach();

        Emit(new UpdateEvent(UpdateStatus.WaitingForProduction, Version: "0.6.0"));
        window.Message.ShouldBe("Üretim sürüyor; iş bitince ya da durdurulunca güncellenecek");
        Emit(new UpdateEvent(UpdateStatus.VerificationFailed, Version: "0.6.0"));
        window.Message.ShouldBe("Güncelleme doğrulanamadı, mevcut sürümle devam ediliyor");
        window.RowButtons.ShouldBeEmpty();
    }

    /// <summary>AC-9: bileşen hatasında satır sade Türkçe sebeple Hata olur; Tekrar dene yalnızca bu satırı yeniden dener; Hatayı bildir 0022 akışını bileşen + adımla açar.</summary>
    /// <returns>Görev.</returns>
    [AvaloniaFact]
    public async Task UpdateWindow_FailedRow_RetryAndReport()
    {
        var reports = Substitute.For<IErrorReportCenter>();
        var window = Attach(reports);

        Emit(new UpdateEvent(UpdateStatus.Failed, Failure: SetupFailure.NoInternet, Version: "0.6.0"));

        window.RowStatusText.ShouldStartWith("Hata — ");
        window.RowButtons.ShouldBe(["Tekrar dene", "Hatayı bildir"]);
        window.ReportFailure();
        reports.Received(1).Raise(Arg.Is<string>(s => s.StartsWith("Güncelleme hatası: ANEW Konsept ajanı — ", StringComparison.Ordinal)), "ANEW Konsept ajanı", InstallStep.Download);
        await window.StartAsync();
        await _updater.Received(1).ApplyAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>AC-14: güncelleme ve kaldırma ekranlarının tüm kontrolleri erişilebilir ada sahiptir; kaldırmada varsayılan (Enter) Hayır'dır.</summary>
    [AvaloniaFact]
    public void UpdateWindow_ControlsHaveAccessibleNames()
    {
        var window = Attach();
        window.AccessibleNames.ShouldAllBe(n => !string.IsNullOrWhiteSpace(n));

        var uninstall = new UninstallWindow();
        uninstall.Attach(Substitute.For<IAgentInstallation>(), "C:\\Videolar");
        uninstall.AccessibleNames.ShouldAllBe(n => !string.IsNullOrWhiteSpace(n));
        uninstall.DefaultAnswer.ShouldBe("Hayır, videoları koru");
    }

    /// <summary>AC-11: kaldırma sorusu Hayır ile videoları korur, Evet ile siler; sonuç "Kaldırıldı".</summary>
    /// <param name="deleteVideos">Cevap.</param>
    /// <returns>Görev.</returns>
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UninstallWindow_DefaultNoFocused(bool deleteVideos)
    {
        var installation = Substitute.For<IAgentInstallation>();
        installation.Uninstall(Arg.Any<bool>(), Arg.Any<string?>()).Returns(true);
        var window = new UninstallWindow();
        window.Attach(installation, "C:\\Videolar\\anewconcept");

        await window.AnswerAsync(deleteVideos);

        installation.Received(1).Uninstall(deleteVideos, "C:\\Videolar\\anewconcept");
        window.Status.ShouldBe("Kaldırıldı");
        window.Finished.ShouldBeTrue();
        await window.AnswerAsync(!deleteVideos);
        installation.ReceivedCalls().Count().ShouldBe(1);
    }

    /// <summary>
    /// AC-2: tray "Güncellemeleri denetle" hemen denetler; yeni sürüm yoksa "Ajan güncel (sürüm X)"; varsa bildirim + "Şimdi güncelle" satırı +
    /// ekran açılır ama güncelleme kullanıcı onayı olmadan başlamaz.
    /// </summary>
    /// <returns>Görev.</returns>
    [AvaloniaFact]
    public async Task Tray_CheckUpdates_UpToDateNotice()
    {
        var notifier = Substitute.For<IDesktopNotifier>();
        var app = CurrentApp;
        app.AttachUpdater(_updater, notifier, rolledBack: true);
        notifier.Received(1).Show(Arg.Any<string>(), "Yeni sürüm başlatılamadı, önceki sürüme dönüldü");
        try
        {
            app.TrayCheckUpdatesItem!.Header.ShouldBe("Güncellemeleri denetle");
            _updater.CheckAsync(Arg.Any<CancellationToken>()).Returns(UpdateCheckOutcome.UpToDate);
            (await app.CheckUpdatesAsync()).ShouldBe(UpdateCheckOutcome.UpToDate);
            notifier.Received(1).Show(Arg.Any<string>(), "Ajan güncel (sürüm 0.5.0)");
            app.TrayUpdateItem!.IsVisible.ShouldBeFalse();

            _updater.CheckAsync(Arg.Any<CancellationToken>()).Returns(UpdateCheckOutcome.Available);
            await app.CheckUpdatesAsync();
            notifier.Received(1).Show(Arg.Any<string>(), "Yeni sürüm hazır: 0.6.0");
            app.TrayUpdateItem.IsVisible.ShouldBeTrue();
            app.TrayUpdateItem.Header.ShouldBe("Yeni sürüm hazır: 0.6.0 — Şimdi güncelle");
            app.UpdateWindow.ShouldNotBeNull();
            app.UpdateWindow.Heading.ShouldBe("Şu an bunları güncelliyoruz");
            await _updater.DidNotReceive().ApplyAsync(Arg.Any<CancellationToken>());
        }
        finally
        {
            app.TrayUpdateItem!.IsVisible = false; // Paylaşılan uygulama: diğer tray testleri değişmesin.
            app.TrayUpdateItem.IsEnabled = false;
            app.UpdateWindow?.Hide();
        }
    }

    /// <summary>
    /// AC-1 (bileşenler): yeni ajan yeni bileşen sürümü getirince kurulum ekranı "Şu an bunları güncelliyoruz" başlığıyla açılır ve yalnızca
    /// değişen bileşeni listeler; bittiğinde "Güncelleme tamamlandı".
    /// </summary>
    /// <returns>Görev.</returns>
    [AvaloniaFact]
    public async Task SetupWindow_UpdateMode_ListsOnlyChanged()
    {
        var temp = Directory.CreateTempSubdirectory("anewconcept-setup-update-");
        try
        {
            var host = new FakeSetupHost();
            var root = Path.Combine(temp.FullName, "anewconcept");
            var alpha = host.Add("alpha");
            var beta = host.Add("beta");
            var store = new FileSetupStateStore(root);
            var first = new SetupCoordinator([alpha, beta], host, store, root, Path.Combine(temp.FullName, "Videolar"), null);
            await first.InspectAsync(CancellationToken.None);
            await first.RunAsync(CancellationToken.None);

            var betaNext = host.Add("beta") with { Version = "2.0.0" };
            var next = new SetupCoordinator([alpha, betaNext], host, store, root, Path.Combine(temp.FullName, "Videolar"), null);
            (await next.InspectAsync(CancellationToken.None)).ShouldBeTrue();
            var window = new SetupWindow();
            window.Attach(next, null, CancellationToken.None);

            window.HeadingDisplay.ShouldBe("Şu an bunları güncelliyoruz");
            window.Title.ShouldBe("ANEW Konsept · Güncelleme");
            window.ListedComponents.ShouldBe(["beta"]);
            await next.RunAsync(CancellationToken.None);
            Dispatcher.UIThread.RunJobs();
            window.DoneDisplay.ShouldBe("Güncelleme tamamlandı");
            window.CloseForGood();
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    private UpdateWindow Attach(IErrorReportCenter? reports = null)
    {
        var window = new UpdateWindow();
        window.Attach(_updater, reports, CancellationToken.None);
        return window;
    }

    private void Emit(UpdateEvent update)
    {
        _updater.Current.Returns(update);
        _updater.Changed += Raise.Event<EventHandler<UpdateEvent>>(_updater, update);
        Dispatcher.UIThread.RunJobs();
    }
}
