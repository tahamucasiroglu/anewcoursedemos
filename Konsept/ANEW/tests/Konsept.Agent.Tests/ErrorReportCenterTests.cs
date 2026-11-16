using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Domain.Rules;
using Konsept.Agent.Infrastructure.Concrete;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Serilog;
using Serilog.Events;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Ajan hata bildirimi mantığı (spec 0022 AC-6–AC-12, AC-15, AC-16): log satırı seçimi, maskeleme/kısaltma, oturumda bir kez, bekletme ve
/// yeniden başlatma, hesap bağı, 20'lik kuyruk, dosyaya kaydetme, sınırda bekletmeme.
/// </summary>
public sealed class ErrorReportCenterTests : IDisposable
{
    private const string Machine = "TAHA-PC";
    private static readonly Guid AccountA = Guid.Parse("0196a000-0000-7000-8000-00000000000a");
    private static readonly Guid AccountB = Guid.Parse("0196a000-0000-7000-8000-00000000000b");
    private static readonly ReportEnvironment Environment = new("Microsoft Windows 10.0.26100", Machine, @"C:\Users\taham");
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 16, 12, 0, 0, TimeSpan.Zero));
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "anewconcept-tests", "reports-" + Guid.NewGuid().ToString("N"));
    private readonly IServerConnection _connection = Substitute.For<IServerConnection>();
    private readonly IRecentLogSource _logs = Substitute.For<IRecentLogSource>();
    private readonly List<LogEvent> _logged = [];

    /// <summary>Initializes a new instance of the <see cref="ErrorReportCenterTests"/> class.</summary>
    public ErrorReportCenterTests()
    {
        _logs.Snapshot().Returns([]);
        _connection.State.Returns(ConnectionState.Disconnected);
    }

    /// <summary>16 dakika önceki satır ve Information satırı eklenmez; 200'den fazla uygun satırda en yeni 200 gider (eskiden yeniye).</summary>
    [Fact]
    public void LogLines_FifteenMinutesWarningPlus_Newest200()
    {
        var now = _time.GetUtcNow();
        var lines = new List<AgentLogLine>
        {
            new(now.AddMinutes(-16), AgentLogLevel.Error, "çok eski hata"),
            new(now.AddMinutes(-1), AgentLogLevel.Information, "bilgi satırı"),
        };
        lines.AddRange(Enumerable.Range(0, 250).Select(i => new AgentLogLine(
            now.AddMinutes(-14).AddSeconds(i),
            (AgentLogLevel)(1 + (i % 3)),
            $"satır {i:000}")));

        var selected = AgentReportText.SelectLogLines(lines, now, Environment);

        selected.Count.ShouldBe(200);
        selected[0].ShouldEndWith("satır 050");
        selected[^1].ShouldEndWith("satır 249");
        string.Join('\n', selected).ShouldNotContain("çok eski");
        string.Join('\n', selected).ShouldNotContain("bilgi satırı");
        selected.ShouldContain(l => l.Contains("[WRN]", StringComparison.Ordinal));
        selected.ShouldContain(l => l.Contains("[ERR]", StringComparison.Ordinal));
        selected.ShouldContain(l => l.Contains("[FTL]", StringComparison.Ordinal));
    }

    /// <summary>
    /// Kullanıcının profil yolu ve başka <c>C:\Users\&lt;ad&gt;</c> yolları <c>%USERPROFILE%</c> olur; makine adı ve 0008 listesindeki
    /// değerler (API key, JWT) rapora girmez.
    /// </summary>
    [Fact]
    public void Sanitize_UserProfileMachineNameAndSecrets()
    {
        const string Raw = @"C:\Users\taham\AppData\Local\Konsept\x.log açılamadı; D:\Users\Ayse\video.mp4; TAHA-PC üzerinde "
            + "sk-proj-AGENTKEY1234567-a1z token eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.c2lnbmF0dXJl";

        var clean = AgentReportText.Sanitize(Raw, Environment);

        clean.ShouldContain(@"%USERPROFILE%\AppData\Local\Konsept\x.log");
        clean.ShouldContain(@"%USERPROFILE%\video.mp4");
        clean.ShouldNotContain("taham", Case.Insensitive);
        clean.ShouldNotContain("Ayse");
        clean.ShouldNotContain(Machine, Case.Insensitive);
        clean.ShouldNotContain("AGENTKEY1234567");
        clean.ShouldNotContain("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0");
    }

    /// <summary>Aynı özet aynı oturumda ikinci kez uyarı göstermez, teknik loga yazılır; ajan yeniden başlayınca yeniden sorulur.</summary>
    [Fact]
    public void Raise_SameSummaryOncePerSession_RestartAsksAgain()
    {
        var center = Create();
        var alerts = 0;
        center.AlertRaised += (_, _) => alerts++;

        var first = center.Raise("FFmpeg bulunamadı");
        var second = center.Raise("FFmpeg bulunamadı");
        var afterRestart = Create().Raise("FFmpeg bulunamadı");

        first.ShouldNotBeNull();
        second.ShouldBeNull();
        alerts.ShouldBe(1);
        _logged.ShouldContain(e => e.Level == LogEventLevel.Information && e.RenderMessage(null).Contains("FFmpeg bulunamadı", StringComparison.Ordinal));
        afterRestart.ShouldNotBeNull();
    }

    /// <summary>
    /// Bağlı değilken "Bildir" bekletir; ajan yeniden başlasa da kaybolmaz; bağlantı + eşleşme kurulunca (iki eşzamanlı akışta bile) tam
    /// bir kez gönderilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Report_Offline_QueuedSurvivesRestart_SentOnce()
    {
        var ct = TestContext.Current.CancellationToken;
        var center = Create();
        var report = center.Raise("render başarısız")!;

        var outcome = await center.ReportAsync(report, ct);
        var restarted = Create();
        Connect(AccountA);
        _connection.TrySendErrorReportAsync(Arg.Any<AgentErrorReport>(), Arg.Any<CancellationToken>()).Returns(ReportOutcome.Sent);
        await Task.WhenAll(restarted.FlushAsync(ct), restarted.FlushAsync(ct));

        outcome.ShouldBe(ReportOutcome.Queued);
        restarted.PendingCount.ShouldBe(0);
        await _connection.Received(1).TrySendErrorReportAsync(Arg.Is<AgentErrorReport>(r => r.Id == report.Id), Arg.Any<CancellationToken>());
        Create().PendingCount.ShouldBe(0);
    }

    /// <summary>A hesabıyla eşleşmişken bekletilen bildirim ajan B ile eşleşince gönderilmez; kullanıcıya "başka hesap" bilgisi verilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Report_OtherAccount_NotSentAndNotice()
    {
        var ct = TestContext.Current.CancellationToken;
        var center = Create();
        var notices = new List<ReportNotice>();
        center.Notice += (_, n) => notices.Add(n);
        Connect(AccountA);
        await center.FlushAsync(ct); // bağlanınca hesap öğrenilir
        _connection.State.Returns(ConnectionState.Disconnected);
        var report = center.Raise("A hesabında hata")!;
        (await center.ReportAsync(report, ct)).ShouldBe(ReportOutcome.Queued);

        Connect(AccountB);
        await center.FlushAsync(ct);

        report.AccountId.ShouldBe(AccountA);
        await _connection.DidNotReceive().TrySendErrorReportAsync(Arg.Any<AgentErrorReport>(), Arg.Any<CancellationToken>());
        notices.ShouldBe([ReportNotice.OtherAccount]);
        center.PendingCount.ShouldBe(0);
    }

    /// <summary>Eşleşme öncesi oluşan bildirim (hesapsız) ilk eşleşen hesap adına gönderilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Report_BeforePairing_GoesWithFirstAccount()
    {
        var ct = TestContext.Current.CancellationToken;
        var center = Create();
        var report = center.Raise("kurulum hatası", "Node.js", InstallStep.Verify)!;
        await center.ReportAsync(report, ct);

        Connect(AccountA);
        _connection.TrySendErrorReportAsync(Arg.Any<AgentErrorReport>(), Arg.Any<CancellationToken>()).Returns(ReportOutcome.Sent);
        await center.FlushAsync(ct);

        report.AccountId.ShouldBeNull();
        report.Component.ShouldBe("Node.js");
        report.Step.ShouldBe("doğrulama");
        await _connection.Received(1).TrySendErrorReportAsync(Arg.Is<AgentErrorReport>(r => r.Id == report.Id && r.AccountId == AccountA), Arg.Any<CancellationToken>());
    }

    /// <summary>20 bekleyen bildirim varken 21. eklenince en eskisi düşer, 20 kalır ve teknik loga Warning yazılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Queue_TwentyFirst_DropsOldestWithWarning()
    {
        var ct = TestContext.Current.CancellationToken;
        var center = Create();
        var reports = new List<AgentErrorReport>();
        for (var i = 0; i < 21; i++)
        {
            var report = center.Raise($"hata {i}")!;
            reports.Add(report);
            await center.ReportAsync(report, ct);
        }

        var stored = new FileErrorReportQueue(_directory).Load().Reports;

        center.PendingCount.ShouldBe(20);
        stored.Count.ShouldBe(20);
        stored.ShouldNotContain(r => r.Id == reports[0].Id);
        stored[^1].Id.ShouldBe(reports[20].Id);
        _logged.ShouldContain(e => e.Level == LogEventLevel.Warning && e.RenderMessage(null).Contains("en eski", StringComparison.Ordinal));
    }

    /// <summary>"Hata raporunu dosyaya kaydet" gönderilecek metnin aynısını (maskeli, makine adsız) yazar; API'ye bir şey gitmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SaveToFile_SameMaskedText_NothingSent()
    {
        var ct = TestContext.Current.CancellationToken;
        var now = _time.GetUtcNow();
        _logs.Snapshot().Returns([new AgentLogLine(now.AddMinutes(-2), AgentLogLevel.Warning, @"C:\Users\taham\a.txt kilitli, anahtar sk-proj-LOGKEY12345678-z9z")]);
        var center = Create();
        var report = center.Raise($"{Machine} üzerinde hata, key sk-proj-SAVEKEY1234567-s1z")!;
        var path = Path.Combine(Directory.CreateDirectory(_directory).FullName, "rapor.txt");

        await center.SaveToFileAsync(report, path, ct);
        var text = await File.ReadAllTextAsync(path, ct);

        text.ShouldBe(AgentReportText.Compose(report));
        text.ShouldContain("%USERPROFILE%");
        text.ShouldNotContain("SAVEKEY1234567");
        text.ShouldNotContain("LOGKEY12345678");
        text.ShouldNotContain(Machine);
        text.ShouldNotContain("taham");
        await _connection.DidNotReceive().TrySendErrorReportAsync(Arg.Any<AgentErrorReport>(), Arg.Any<CancellationToken>());
        center.PendingCount.ShouldBe(0);
    }

    /// <summary>Sınır aşıldığında ("Çok fazla bildirim…") bildirim bekletmeye alınmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Report_RateLimited_NotQueued()
    {
        var ct = TestContext.Current.CancellationToken;
        var center = Create();
        Connect(AccountA);
        _connection.TrySendErrorReportAsync(Arg.Any<AgentErrorReport>(), Arg.Any<CancellationToken>()).Returns(ReportOutcome.RateLimited);

        var outcome = await center.ReportAsync(center.Raise("sınır hatası")!, ct);

        outcome.ShouldBe(ReportOutcome.RateLimited);
        center.PendingCount.ShouldBe(0);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Geçici klasör.
        }
    }

    private void Connect(Guid account)
    {
        _connection.State.Returns(ConnectionState.Connected);
        _connection.TryGetAccountAsync(Arg.Any<CancellationToken>()).Returns(account);
    }

    private ErrorReportCenter Create()
    {
        var info = Substitute.For<IAgentInfoProvider>();
        info.Version.Returns("0.9.0");
        info.MachineName.Returns(Machine);
        var logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(new DelegateSink(e =>
        {
            lock (_logged)
            {
                _logged.Add(e);
            }
        })).CreateLogger();
        return new ErrorReportCenter(_connection, new FileErrorReportQueue(_directory), _logs, info, Environment, _time, logger);
    }
}
