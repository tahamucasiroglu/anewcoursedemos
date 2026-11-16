using Avalonia.Headless.XUnit;
using Konsept.Agent.Abstract;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Domain.Rules;
using NSubstitute;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Ajan hata uyarısı arayüzü (spec 0022 AC-5, AC-8, AC-9, AC-11, AC-12) — headless: özet, düğmeler ve erişilebilir adları, ayrıntılar,
/// sonuç metinleri, dosyaya kaydetme, "Kapat"ın hiçbir şey göndermemesi, Windows bildirimi ve tray işareti.
/// </summary>
public sealed class ErrorAlertWindowTests
{
    private static App CurrentApp => (App)Avalonia.Application.Current!;

    /// <summary>
    /// Pencere maskelenmiş özeti, "Bildir", "Hata raporunu dosyaya kaydet", "Kapat" ve "Ayrıntıları göster"i (erişilebilir adlarıyla)
    /// gösterir; ayrıntılar gönderilecek metnin tamamıdır; "Bildir" sonucu (bekletme) gösterilir; kaydetme seçilen dosyaya yazdırır.
    /// </summary>
    /// <returns>Görev.</returns>
    [AvaloniaFact]
    public async Task ErrorAlertWindow_ShowsSummaryButtonsAndDetails()
    {
        var center = Substitute.For<IErrorReportCenter>();
        center.ReportAsync(Arg.Any<AgentErrorReport>(), Arg.Any<CancellationToken>()).Returns(ReportOutcome.Queued);
        var report = Report();
        var window = new ErrorAlertWindow();
        window.Attach(center, report, () => Task.FromResult<string?>(@"C:\rapor.txt"));

        window.SummaryDisplay.ShouldBe(report.Summary);
        window.ButtonNames.ShouldBe(["Bildir", "Hata raporunu dosyaya kaydet", "Kapat", "Ayrıntıları göster"]);
        window.DetailsDisplay.ShouldBeEmpty();
        window.ToggleDetails();
        window.DetailsDisplay.ShouldBe(AgentReportText.Compose(report));
        window.ButtonNames[3].ShouldBe("Ayrıntıları gizle");

        await window.ReportAsync();
        window.StatusDisplay.ShouldBe("Bağlantı kurulunca gönderilecek");
        window.ReportEnabled.ShouldBeFalse();
        await window.SaveAsync();
        await center.Received(1).SaveToFileAsync(report, @"C:\rapor.txt", Arg.Any<CancellationToken>());
        window.StatusDisplay.ShouldBe(@"Rapor kaydedildi: C:\rapor.txt");
        ErrorAlertWindow.OutcomeLabel(ReportOutcome.Sent).ShouldStartWith("Bildiriminiz gönderildi");
        ErrorAlertWindow.OutcomeLabel(ReportOutcome.OtherAccount).ShouldBe("Bu bildirim başka bir hesapta oluşturuldu, gönderilmedi");
        ErrorAlertWindow.OutcomeLabel(ReportOutcome.RateLimited).ShouldBe("Çok fazla bildirim gönderdiniz, biraz sonra tekrar deneyin");
    }

    /// <summary>"Kapat" hiçbir şey göndermez ve kaydetmez.</summary>
    [AvaloniaFact]
    public void Dismiss_SendsNothing()
    {
        var center = Substitute.For<IErrorReportCenter>();
        var window = new ErrorAlertWindow();
        window.Attach(center, Report(), () => Task.FromResult<string?>(null));
        window.Show();

        window.Close();

        center.DidNotReceive().ReportAsync(Arg.Any<AgentErrorReport>(), Arg.Any<CancellationToken>());
        center.DidNotReceive().SaveToFileAsync(Arg.Any<AgentErrorReport>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Hata olunca uyarı penceresi açılır, Windows bildirimi gösterilir ve tray ikonunda bekleyen bildirim işareti ("Hata uyarısını göster")
    /// görünür; pencere kapanınca ve bekleyen yoksa işaret kalkar; bekleyen bildirim varken işaret kalır.
    /// </summary>
    [AvaloniaFact]
    public void Alert_RaisesNotifierAndTrayMarker()
    {
        var center = Substitute.For<IErrorReportCenter>();
        var notifier = Substitute.For<IDesktopNotifier>();
        var report = Report();
        CurrentApp.AttachErrorReports(center, notifier);

        try
        {
            var window = CurrentApp.ShowAlert(report);

            window.IsVisible.ShouldBeTrue();
            CurrentApp.AlertWindow.ShouldBeSameAs(window);
            notifier.Received(1).Show("ANEW Konsept: Bir hata oldu", report.Summary);
            CurrentApp.TrayMarkerShown.ShouldBeTrue();
            CurrentApp.TrayPendingItem!.IsVisible.ShouldBeTrue();
            CurrentApp.TrayPendingItem.IsEnabled.ShouldBeTrue();

            window.Close();
            CurrentApp.TrayMarkerShown.ShouldBeFalse();
            CurrentApp.TrayPendingItem.IsVisible.ShouldBeFalse();

            CurrentApp.ShowPending(2);
            CurrentApp.TrayMarkerShown.ShouldBeTrue();
            CurrentApp.TrayPendingItem.Header.ShouldBe("Hata uyarısını göster (2 bekleyen bildirim)");
        }
        finally
        {
            CurrentApp.AlertWindow?.Close();
            CurrentApp.ShowPending(0);
        }
    }

    private static AgentErrorReport Report() => new(
        Guid.NewGuid(),
        DateTimeOffset.UtcNow,
        null,
        "FFmpeg indirilemedi (HttpRequestException: 404)",
        "0.9.0",
        "Microsoft Windows 10.0.26100",
        "FFmpeg",
        "indirme",
        ["2026-11-16T12:00:00Z [WRN] yeniden deneniyor"]);
}
