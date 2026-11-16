using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Logging;
using Konsept.Agent.Infrastructure.Setup;
using NSubstitute;
using Serilog;
using Serilog.Events;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Kurulum motoru (spec 0009 AC-5, AC-6, AC-10–13, AC-15–17, AC-19, AC-20, AC-22): sahte indirici, gerçek hash/zip, geçici kurulum
/// yeri. Gerçek ağ çağrısı yok.
/// </summary>
public sealed class SetupCoordinatorTests : IDisposable
{
    private static readonly Uri Mirror = new("https://pi.invalid/ajan-bilesenleri/");
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("anewconcept-setup-");
    private readonly FakeSetupHost _host = new();
    private readonly List<SetupEvent> _events = [];
    private readonly SetupComponent _alpha;
    private readonly SetupComponent _beta;

    /// <summary>Initializes a new instance of the <see cref="SetupCoordinatorTests"/> class.</summary>
    public SetupCoordinatorTests()
    {
        _alpha = _host.Add("alpha", mirrored: true);
        _beta = _host.Add("beta");
    }

    private string Root => Path.Combine(_root.FullName, "anewconcept");

    /// <inheritdoc />
    public void Dispose() => _root.Delete(recursive: true);

    /// <summary>AC-5, AC-17: her bileşen Bekliyor → İndiriliyor → Doğrulanıyor → Kuruluyor → Kuruldu sırasını izler; sonunda Hazır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_StatusSequence_AndEvents()
    {
        var setup = await CreateAsync();
        setup.Components.Select(c => c.Status).ShouldBe([ComponentStatus.Waiting, ComponentStatus.Waiting]);

        await setup.RunAsync(TestContext.Current.CancellationToken);

        StatusesOf("alpha").ShouldBe([ComponentStatus.Downloading, ComponentStatus.Verifying, ComponentStatus.Installing, ComponentStatus.Installed]);
        StatusesOf("beta").ShouldBe([ComponentStatus.Downloading, ComponentStatus.Verifying, ComponentStatus.Installing, ComponentStatus.Installed]);
        _events[0].Kind.ShouldBe(SetupEventKind.Started);
        _events.Select(e => e.Kind).ShouldContain(SetupEventKind.Checking);
        _events[^1].Kind.ShouldBe(SetupEventKind.Ready);
        setup.IsReady.ShouldBeTrue();
        setup.Percent.ShouldBe(100);
        File.Exists(Path.Combine(Root, "components", "alpha", "bin", "tool.exe")).ShouldBeTrue();
        Directory.Exists(Path.Combine(Root, "components", "alpha.partial")).ShouldBeFalse();
        Directory.Exists(Path.Combine(Root, "downloads")).ShouldBeTrue();
        Directory.GetFiles(Path.Combine(Root, "downloads")).ShouldBeEmpty();
        File.Exists(Path.Combine(Root, FileSetupStateStore.FileName)).ShouldBeTrue();
    }

    /// <summary>AC-6: SHA-256'sı uyuşmayan (kasıtlı bozulmuş) dosya kurulmaz, "Dosya doğrulanamadı"; bozma kalkınca Tekrar dene kurar.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_HashMismatch_NotInstalledThenRetryInstalls()
    {
        var good = _host.Content[_alpha.Url];
        _host.Content[_alpha.Url] = [.. good, 0x00];
        var setup = await CreateAsync();

        await setup.RunAsync(TestContext.Current.CancellationToken);

        var alpha = State(setup, "alpha");
        alpha.Status.ShouldBe(ComponentStatus.Failed);
        alpha.Failure.ShouldBe(SetupFailure.VerifyFailed);
        alpha.FailedStep.ShouldBe(InstallStep.Verify);
        Directory.Exists(Path.Combine(Root, "components", "alpha")).ShouldBeFalse();
        State(setup, "beta").Status.ShouldBe(ComponentStatus.Installed);
        setup.IsReady.ShouldBeFalse();

        _host.Content[_alpha.Url] = good;
        await setup.RetryAsync("alpha", TestContext.Current.CancellationToken);

        State(setup, "alpha").Status.ShouldBe(ComponentStatus.Installed);
        setup.IsReady.ShouldBeTrue();
        _host.Downloads.Count(u => u == _beta.Url).ShouldBe(1);
    }

    /// <summary>AC-11: internet yokken ya da boş alan (indirme + açılmış × 1,2) azken ön kontrol geçmez; kurulanlar düşünce gereken azalır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Preflight_OfflineOrDiskShort_BlocksWithReason()
    {
        var setup = await CreateAsync();
        var preflight = setup.Preflight();
        preflight.RequiredBytes.ShouldBe(7200);
        preflight.Passed.ShouldBeTrue();

        _host.Online = false;
        setup.Preflight().Online.ShouldBeFalse();
        setup.Preflight().Passed.ShouldBeFalse();

        _host.Online = true;
        _host.Free = 7199;
        setup.Preflight().EnoughSpace.ShouldBeFalse();
        setup.Preflight().Passed.ShouldBeFalse();

        _host.Unreachable.Add(_beta.Url);
        await setup.RunAsync(TestContext.Current.CancellationToken);
        setup.Preflight().RequiredBytes.ShouldBe(3600);
    }

    /// <summary>AC-11: kurulum sırasında internet koparsa işlenen bileşen "İnternet bağlantısı koptu" olur; tamamlanan Kuruldu kalır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_ConnectionLost_FailsNoInternet_KeepsInstalled()
    {
        _host.BeforeDownload = (url, _) =>
        {
            if (url == _beta.Url)
            {
                _host.Online = false;
                _host.Unreachable.Add(url);
            }
        };
        var setup = await CreateAsync();

        await setup.RunAsync(TestContext.Current.CancellationToken);

        State(setup, "alpha").Status.ShouldBe(ComponentStatus.Installed);
        State(setup, "beta").Failure.ShouldBe(SetupFailure.NoInternet);
        State(setup, "beta").FailedStep.ShouldBe(InstallStep.Download);
    }

    /// <summary>AC-12: resmi kaynak erişilemezken 2 otomatik yeniden denemeden sonra Pi aynasından kurulur; denemeler olay olarak akar.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Download_TwoRetriesThenMirror()
    {
        _host.Unreachable.Add(_alpha.Url);
        var mirrorUrl = new Uri(Mirror, _alpha.FileName);
        _host.Content[mirrorUrl] = _host.Content[_alpha.Url];
        var setup = await CreateAsync();

        await setup.RunAsync(TestContext.Current.CancellationToken);

        _host.Downloads.Where(u => u != _beta.Url).ShouldBe([_alpha.Url, _alpha.Url, _alpha.Url, mirrorUrl]);
        _events.Where(e => e.Kind == SetupEventKind.Retrying).Select(e => e.Attempt).ShouldBe([1, 2]);
        _events.Count(e => e.Kind == SetupEventKind.UsingMirror).ShouldBe(1);
        State(setup, "alpha").Status.ShouldBe(ComponentStatus.Installed);
    }

    /// <summary>AC-12: aynası olmayan bileşen 3. başarısızlıktan sonra Hata olur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Download_NoMirror_FailsAfterThirdAttempt()
    {
        _host.Unreachable.Add(_beta.Url);
        var setup = await CreateAsync();

        await setup.RunAsync(TestContext.Current.CancellationToken);

        _host.Downloads.Count(u => u == _beta.Url).ShouldBe(3);
        State(setup, "beta").Status.ShouldBe(ComponentStatus.Failed);
        State(setup, "beta").Failure.ShouldBe(SetupFailure.DownloadFailed);
        _events[^1].Kind.ShouldBe(SetupEventKind.Stopped);
    }

    /// <summary>AC-13: Tekrar dene yalnızca hatalı bileşeni yeniden dener; Kuruldu olan yeniden indirilmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Retry_OnlyFailedComponent_InstalledNotRedownloaded()
    {
        _host.Unreachable.Add(_beta.Url);
        var setup = await CreateAsync();
        await setup.RunAsync(TestContext.Current.CancellationToken);

        _host.Unreachable.Clear();
        await setup.RetryAsync("beta", TestContext.Current.CancellationToken);

        _host.Downloads.Count(u => u == _alpha.Url).ShouldBe(1);
        State(setup, "beta").Status.ShouldBe(ComponentStatus.Installed);
        setup.IsReady.ShouldBeTrue();
    }

    /// <summary>AC-15: yarıda kalan kurulumdan sonra açılışta yarım dosyalar silinir, Kuruldu korunur, kaldığı yerden sürer.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Interrupted_PartialDeleted_InstalledKept_Resumes()
    {
        _host.Unreachable.Add(_beta.Url);
        await (await CreateAsync()).RunAsync(TestContext.Current.CancellationToken);
        var partial = Directory.CreateDirectory(Path.Combine(Root, "components", "beta.partial", "x"));
        await File.WriteAllTextAsync(Path.Combine(Root, "downloads", "beta.zip"), "yarım", TestContext.Current.CancellationToken);
        _host.Unreachable.Clear();
        _host.Downloads.Clear();

        var reopened = await CreateAsync();

        Directory.Exists(Path.Combine(Root, "components", "beta.partial")).ShouldBeFalse();
        partial.Exists.ShouldBeFalse();
        File.Exists(Path.Combine(Root, "downloads", "beta.zip")).ShouldBeFalse();
        State(reopened, "alpha").Status.ShouldBe(ComponentStatus.Installed);
        State(reopened, "beta").Status.ShouldBe(ComponentStatus.Waiting);
        await reopened.RunAsync(TestContext.Current.CancellationToken);
        _host.Downloads.ShouldBe([_beta.Url]);
        reopened.IsReady.ShouldBeTrue();
    }

    /// <summary>AC-16: kurulum durdurulunca (tray "Çıkış") yarım bileşenin dosyaları silinir, tamamlananlar korunur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Cancel_DeletesPartial_KeepsInstalled()
    {
        using var cts = new CancellationTokenSource();
        _host.BeforeDownload = (url, destination) =>
        {
            if (url == _beta.Url)
            {
                File.WriteAllText(destination, "yarım");
                cts.Cancel();
            }
        };
        var setup = await CreateAsync();

        await setup.RunAsync(cts.Token);

        State(setup, "alpha").Status.ShouldBe(ComponentStatus.Installed);
        State(setup, "beta").Status.ShouldBe(ComponentStatus.Waiting);
        File.Exists(Path.Combine(Root, "downloads", "beta.zip")).ShouldBeFalse();
        _events[^1].Kind.ShouldBe(SetupEventKind.Stopped);
        setup.IsRunning.ShouldBeFalse();
    }

    /// <summary>AC-17: Hazır denetiminde sabit sürümü vermeyen araç "Ajan kurulumu eksik" sebebiyle Hata olur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ReadyCheck_VersionMismatch_FailsWithTool()
    {
        _host.Run = (exe, _) => exe.Contains("alpha", StringComparison.Ordinal) ? "v0.9.0" : "v1.0.0";
        var setup = await CreateAsync();

        await setup.RunAsync(TestContext.Current.CancellationToken);

        var alpha = State(setup, "alpha");
        alpha.Status.ShouldBe(ComponentStatus.Failed);
        alpha.Failure.ShouldBe(SetupFailure.ReadyCheckFailed);
        State(setup, "beta").Status.ShouldBe(ComponentStatus.Installed);
        setup.IsReady.ShouldBeFalse();
    }

    /// <summary>AC-19: her şey kurulu ve sağlamken yeniden açılışta kurulum ekranı gerekmez, hiçbir şey indirilmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Reopen_AllInstalled_NoSetupNoDownload()
    {
        await (await CreateAsync()).RunAsync(TestContext.Current.CancellationToken);
        _host.Downloads.Clear();

        var reopened = Create();
        var needsSetup = await reopened.InspectAsync(TestContext.Current.CancellationToken);

        needsSetup.ShouldBeFalse();
        reopened.IsReady.ShouldBeTrue();
        _host.Downloads.ShouldBeEmpty();
    }

    /// <summary>AC-20: değiştirilen ya da silinen exe "Onarılmalı" olur; Onar yalnızca o bileşeni yeniden indirip kurar.</summary>
    /// <param name="delete">Silinsin mi (yoksa değiştirilir).</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Integrity_TamperedExe_NeedsRepair_RepairOnlyThat(bool delete)
    {
        await (await CreateAsync()).RunAsync(TestContext.Current.CancellationToken);
        var exe = Path.Combine(Root, "components", "alpha", "bin", "tool.exe");
        if (delete)
        {
            File.Delete(exe);
        }
        else
        {
            await File.AppendAllTextAsync(exe, "değişti", TestContext.Current.CancellationToken);
        }

        _host.Downloads.Clear();
        var reopened = Create();
        (await reopened.InspectAsync(TestContext.Current.CancellationToken)).ShouldBeTrue();

        State(reopened, "alpha").Status.ShouldBe(ComponentStatus.NeedsRepair);
        State(reopened, "beta").Status.ShouldBe(ComponentStatus.Installed);
        await reopened.RetryAsync("alpha", TestContext.Current.CancellationToken);
        _host.Downloads.ShouldBe([_alpha.Url]);
        reopened.IsReady.ShouldBeTrue();
    }

    /// <summary>AC-10: video klasörü varsayılan Videolar\anewconcept; değiştirilebilir ve saklanır; kurulum yeri (ya da içi) olamaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task VideoFolder_DefaultChangeable_NotInsideInstallRoot()
    {
        var setup = await CreateAsync();
        setup.VideoFolder.ShouldBe(Path.Combine(_root.FullName, "Videolar", "anewconcept"));

        setup.TrySetVideoFolder(Root).ShouldBeFalse();
        setup.TrySetVideoFolder(Path.Combine(Root, "videolar")).ShouldBeFalse();
        setup.TrySetVideoFolder(_root.FullName).ShouldBeFalse();
        setup.TrySetVideoFolder("göreli\\klasör").ShouldBeFalse();
        var chosen = Path.Combine(_root.FullName, "Benim Videolarım");
        setup.TrySetVideoFolder(chosen).ShouldBeTrue();

        (await CreateAsync()).VideoFolder.ShouldBe(chosen);
    }

    /// <summary>
    /// AC-22: kurulum başlangıcı, bileşen kuruldu, yeniden deneme, hata, doğrulama başarısızlığı, Onarılmalı ve Hazır uygun seviyede
    /// loglanır; kurulum hatası 0022 uyarısını kendiliğinden açmaz (kullanıcı "Hatayı bildir" ile açar).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Setup_LogsEventsAtLevels_NoAlert()
    {
        var events = new List<LogEvent>();
        var center = Substitute.For<IErrorReportCenter>();
        var alert = new ErrorAlertSink();
        alert.Attach(center);
        using var logger = new LoggerConfiguration().WriteTo.Sink(new DelegateSink(events.Add)).WriteTo.Sink(alert).CreateLogger();
        _host.Unreachable.Add(_beta.Url);
        var good = _host.Content[_alpha.Url];
        _host.Content[_alpha.Url] = [.. good, 1];
        var setup = Create(logger);
        await setup.InspectAsync(TestContext.Current.CancellationToken);

        await setup.RunAsync(TestContext.Current.CancellationToken);
        _host.Content[_alpha.Url] = good;
        _host.Unreachable.Clear();
        await setup.RunAsync(TestContext.Current.CancellationToken);
        await File.AppendAllTextAsync(Path.Combine(Root, "components", "beta", "bin", "tool.exe"), "x", TestContext.Current.CancellationToken);
        await Create(logger).InspectAsync(TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        var lines = events.Select(e => (e.Level, Text: e.RenderMessage(System.Globalization.CultureInfo.InvariantCulture))).ToList();
        lines.ShouldContain(l => l.Level == LogEventLevel.Information && l.Text.StartsWith("Kurulum başladı", StringComparison.Ordinal));
        lines.ShouldContain(l => l.Level == LogEventLevel.Warning && l.Text.Contains("yeniden deneniyor", StringComparison.Ordinal));
        lines.ShouldContain(l => l.Level == LogEventLevel.Error && l.Text.Contains("doğrulanamadı", StringComparison.Ordinal));
        lines.ShouldContain(l => l.Level == LogEventLevel.Error && l.Text.Contains("indirilemedi", StringComparison.Ordinal));
        lines.ShouldContain(l => l.Level == LogEventLevel.Information && l.Text.Contains("bileşen kuruldu", StringComparison.Ordinal));
        lines.ShouldContain(l => l.Level == LogEventLevel.Information && l.Text.Contains("Hazır", StringComparison.Ordinal));
        lines.ShouldContain(l => l.Level == LogEventLevel.Warning && l.Text.Contains("onarılmalı", StringComparison.Ordinal));
        events.Where(e => e.Level == LogEventLevel.Error).ShouldAllBe(e => e.Properties.ContainsKey(ErrorAlertSink.NoAlertProperty));
        center.DidNotReceiveWithAnyArgs().Raise(default!);
    }

    /// <summary>
    /// Spec 0018 / B-118: npm bileşeni (HyperFrames) indirme/arşiv adımı olmadan, kurulu Node klasörü ve katalogdaki kilit hash'iyle kurulur;
    /// yürütülebilir dosyası kaydedilir, Hazır olur. npm başarısızsa "kurulamadı", kısmi klasör kalmaz, Tekrar dene kurar.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Install_NpmComponent_UsesNodeAndLock_NoDownload()
    {
        var hf = _host.AddNpm("hyperframes");
        _host.PackagesSucceed = false;
        var setup = Create(components: [_alpha, hf]);
        await setup.InspectAsync(TestContext.Current.CancellationToken);
        await setup.RunAsync(TestContext.Current.CancellationToken);

        State(setup, "hyperframes").Status.ShouldBe(ComponentStatus.Failed);
        Directory.Exists(Path.Combine(Root, "components", "hyperframes.partial")).ShouldBeFalse();
        setup.IsReady.ShouldBeFalse();

        _host.PackagesSucceed = true;
        await setup.RetryAsync("hyperframes", TestContext.Current.CancellationToken);

        State(setup, "hyperframes").Status.ShouldBe(ComponentStatus.Installed);
        _host.PackageInstalls.ShouldAllBe(p => p.Id == "hyperframes" && p.Lock == hf.Sha256 && p.Node == Path.Combine(Root, "components", "node"));
        _host.Downloads.ShouldNotContain(hf.Url);
        File.Exists(Path.Combine(Root, "components", "hyperframes", "bin", "tool.mjs")).ShouldBeTrue();
        setup.IsReady.ShouldBeTrue();
    }

    private SetupCoordinator Create(ILogger? logger = null, IReadOnlyList<SetupComponent>? components = null)
    {
        var setup = new SetupCoordinator(
            components ?? [_alpha, _beta],
            _host,
            new FileSetupStateStore(Root),
            Root,
            Path.Combine(_root.FullName, "Videolar", "anewconcept"),
            Mirror,
            logger);
        setup.Changed += (_, e) => _events.Add(e);
        return setup;
    }

    private async Task<SetupCoordinator> CreateAsync()
    {
        var setup = Create();
        await setup.InspectAsync(TestContext.Current.CancellationToken);
        return setup;
    }

    private static ComponentState State(ISetupCoordinator setup, string id) =>
        setup.Components.Single(c => c.Component.Id == id);

    private List<ComponentStatus> StatusesOf(string id) =>
        [.. _events.Where(e => e.Kind == SetupEventKind.StatusChanged && e.State!.Component.Id == id).Select(e => e.State!.Status)];
}
