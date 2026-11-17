using System.Text;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Application.Base;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Update;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using Serilog;
using Serilog.Events;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Ajan güncellemesi (spec 0023 AC-1…AC-5, AC-7, AC-8, AC-13): imzalı liste (sahte sunucu, test anahtarı), eski/aynı sürüm reddi, kasıtlı bozulmuş
/// imza ve SHA-256, onaysız güncelleme yok, "Üretiliyor" iş varken bekleme, değiş-tokuş + tek önceki sürüm, ilk açılış onayı ve geri dönüş.
/// Tüm dosyalar geçici klasörde; kayıt defteri, kısayol ve süreçler sahte — kurulu ajana dokunulmaz.
/// </summary>
public sealed class AgentUpdaterTests : IDisposable
{
    private const string Current = "0.5.0";
    private static readonly Uri Feed = new("https://pi.invalid/ajan-guncelleme/");
    private readonly DirectoryInfo _temp = Directory.CreateTempSubdirectory("anewconcept-update-");
    private readonly FakeSetupHost _host = new();
    private readonly RecordingInstallHost _install = new();
    private readonly FakeTimeProvider _time = new();
    private readonly IServerConnection _connection = Substitute.For<IServerConnection>();
    private readonly List<LogEvent> _logs = [];
    private readonly List<UpdateEvent> _events = [];
    private readonly Ed25519PrivateKeyParameters _key;
    private readonly string _publicKey;
    private readonly AgentInstallation _installation;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="AgentUpdaterTests"/> class.</summary>
    public AgentUpdaterTests()
    {
        (_key, _publicKey) = NewKey();
        _installation = new AgentInstallation(Path.Combine(_temp.FullName, "anewconcept"), Path.Combine(_temp.FullName, "Başlat"), _install);
        Directory.CreateDirectory(Path.GetDirectoryName(_installation.InstalledExe)!);
        File.WriteAllText(_installation.InstalledExe, "eski sürüm");
        _logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(new DelegateSink(_logs.Add)).CreateLogger();
        _connection.TryBeginUpdateAsync(Arg.Any<CancellationToken>()).Returns(true);
    }

    private static byte[] NewExe => Encoding.UTF8.GetBytes("yeni sürüm exe");

    /// <inheritdoc />
    public void Dispose()
    {
        (_logger as IDisposable)?.Dispose();
        _temp.Delete(recursive: true);
    }

    /// <summary>
    /// AC-1: imzalı yeni sürüm bulunur; yalnızca onayla (<see cref="IAgentUpdater.ApplyAsync"/>) indirilir, SHA-256 doğrulanır, kurulur, önceki
    /// sürüm saklanır ve yeni sürüm "--guncelleme-sonrasi" ile başlatılır; olaylar İndiriliyor → Doğrulanıyor → Kuruluyor → Tamamlandı.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Check_NewerSigned_AvailableThenApplySwapsAndRelaunches()
    {
        Publish("0.6.0", NewExe);
        var updater = Create();
        var restarted = false;
        updater.RestartRequested += (_, _) => restarted = true;
        var ct = TestContext.Current.CancellationToken;

        (await updater.CheckAsync(ct)).ShouldBe(UpdateCheckOutcome.Available);
        updater.Available!.Version.ShouldBe("0.6.0");
        _host.Downloads.ShouldNotContain(ExeUri("0.6.0")); // Onay yokken indirme yok (AC-2).

        (await updater.ApplyAsync(ct)).ShouldBeTrue();

        File.ReadAllBytes(_installation.InstalledExe).ShouldBe(NewExe);
        File.ReadAllText(_installation.PreviousExe).ShouldBe("eski sürüm");
        _installation.LoadState().ShouldBe(new UpdateState("0.6.0", 0, Current));
        _install.Launches.ShouldBe([(_installation.InstalledExe, AgentUpdaterBase.AfterUpdateArgument)]);
        restarted.ShouldBeTrue();
        await _connection.DidNotReceive().TryEndUpdateAsync();
        _events.Select(e => e.Status).Distinct().ShouldBe(
            [UpdateStatus.Available, UpdateStatus.Downloading, UpdateStatus.Verifying, UpdateStatus.Installing, UpdateStatus.Completed]);
        await _connection.Received(1).TryBeginUpdateAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>AC-2, AC-5: aynı ya da eski sürümü taşıyan geçerli imzalı liste reddedilir — "güncel", hiçbir dosya indirilmez, Warning loglanır.</summary>
    /// <param name="offered">Listedeki sürüm.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData("0.5.0")]
    [InlineData("0.4.9")]
    [InlineData("0.5")]
    public async Task Check_OlderOrSameSigned_RejectedWarning(string offered)
    {
        Publish(offered, NewExe);
        var updater = Create();
        var ct = TestContext.Current.CancellationToken;

        (await updater.CheckAsync(ct)).ShouldBe(UpdateCheckOutcome.UpToDate);
        updater.Available.ShouldBeNull();
        (await updater.ApplyAsync(ct)).ShouldBeFalse();
        _host.Downloads.ShouldNotContain(ExeUri(offered));
        File.ReadAllText(_installation.InstalledExe).ShouldBe("eski sürüm");
        _logs.ShouldContain(e => e.Level == LogEventLevel.Warning && e.MessageTemplate.Text.StartsWith("Eski ya da aynı sürümü taşıyan liste reddedildi", StringComparison.Ordinal));
    }

    /// <summary>
    /// AC-4 (kasıtlı bozma): başka anahtarla imzalanmış ya da imzadan sonra değiştirilmiş liste kurulmaz — "doğrulanamadı", Error, exe indirilmez.
    /// </summary>
    /// <param name="tamper">Bozma biçimi.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData("other-key")]
    [InlineData("edited")]
    [InlineData("truncated-signature")]
    public async Task Check_BadSignature_VerificationFailed_ErrorLogged(string tamper)
    {
        Publish("0.6.0", NewExe);
        var manifest = Feed + "manifest.json";
        var signature = new Uri(Feed, "manifest.json.sig");
        switch (tamper)
        {
            case "other-key":
                _host.Content[signature] = Sign(_host.Content[new Uri(manifest)], NewKey().Key);
                break;
            case "edited":
                _host.Content[new Uri(manifest)] = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(_host.Content[new Uri(manifest)]).Replace("0.6.0", "0.7.0", StringComparison.Ordinal));
                break;
            default:
                _host.Content[signature] = _host.Content[signature][..32];
                break;
        }

        var updater = Create();
        var ct = TestContext.Current.CancellationToken;

        (await updater.CheckAsync(ct)).ShouldBe(UpdateCheckOutcome.VerificationFailed);
        updater.Available.ShouldBeNull();
        (await updater.ApplyAsync(ct)).ShouldBeFalse();
        _events[^1].Status.ShouldBe(UpdateStatus.VerificationFailed);
        _host.Downloads.ShouldNotContain(ExeUri("0.6.0"));
        File.ReadAllText(_installation.InstalledExe).ShouldBe("eski sürüm");
        _logs.ShouldContain(e => e.Level == LogEventLevel.Error && e.MessageTemplate.Text.StartsWith("Güncelleme doğrulanamadı", StringComparison.Ordinal));
    }

    /// <summary>AC-4 (kasıtlı bozma): imza geçerli ama indirilen exe'nin SHA-256'sı tutmuyor → kurulmaz, geçici dosya silinir, Error.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Apply_HashMismatch_NothingInstalled()
    {
        Publish("0.6.0", NewExe);
        _host.Content[ExeUri("0.6.0")] = Encoding.UTF8.GetBytes("araya girilmiş exe");
        var updater = Create();
        var ct = TestContext.Current.CancellationToken;
        await updater.CheckAsync(ct);

        (await updater.ApplyAsync(ct)).ShouldBeFalse();

        _events[^1].Status.ShouldBe(UpdateStatus.VerificationFailed);
        File.ReadAllText(_installation.InstalledExe).ShouldBe("eski sürüm");
        File.Exists(_installation.PreviousExe).ShouldBeFalse();
        Directory.GetFiles(_installation.StagingDirectory).ShouldBeEmpty();
        _installation.LoadState().ShouldBe(new UpdateState());
        _install.Launches.ShouldBeEmpty();
        _logs.ShouldContain(e => e.Level == LogEventLevel.Error && e.MessageTemplate.Text.Contains("SHA-256", StringComparison.Ordinal));
        await _connection.Received(1).TryEndUpdateAsync(); // sunucu ajana yeniden iş gönderir (review r1 H-1)
    }

    /// <summary>
    /// AC-3: sunucu "Üretiliyor" iş bildirdikçe güncelleme başlamaz (bekleme durumu), 15 sn arayla yeniden sorulur ve iş bitince kendiliğinden başlar.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Apply_WhileProducing_WaitsThenStartsAutomatically()
    {
        Publish("0.6.0", NewExe);
        _connection.TryBeginUpdateAsync(Arg.Any<CancellationToken>()).Returns(false, false, true);
        var updater = Create();
        var ct = TestContext.Current.CancellationToken;
        await updater.CheckAsync(ct);

        var apply = updater.ApplyAsync(ct);
        await WaitUntilAsync(() => updater.Current.Status == UpdateStatus.WaitingForProduction, ct);
        _host.Downloads.ShouldNotContain(ExeUri("0.6.0"));
        while (!apply.IsCompleted)
        {
            _time.Advance(TimeSpan.FromSeconds(15));
            await Task.Delay(20, ct);
        }

        (await apply).ShouldBeTrue();
        await _connection.Received(3).TryBeginUpdateAsync(Arg.Any<CancellationToken>());
        _events.Count(e => e.Status == UpdateStatus.WaitingForProduction).ShouldBe(1);
        File.ReadAllBytes(_installation.InstalledExe).ShouldBe(NewExe);
    }

    /// <summary>AC-1, AC-2: zamanlanmış denetim açılışta ve 6 saatte bir çalışır; yeni sürümü yalnızca haber verir, kendiliğinden kurmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Check_Scheduled_EverySixHours_NeverApplies()
    {
        Publish("0.6.0", NewExe);
        var updater = Create();
        var found = new List<string>();
        updater.AvailableFound += (_, m) => found.Add(m.Version);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        var schedule = updater.RunScheduleAsync(stop.Token);
        await WaitUntilAsync(() => found.Count == 1, stop.Token);
        _time.Advance(TimeSpan.FromHours(5));
        await Task.Delay(50, stop.Token);
        found.Count.ShouldBe(1);
        _time.Advance(TimeSpan.FromHours(1));
        await WaitUntilAsync(() => found.Count == 2, stop.Token);
        await stop.CancelAsync();
        await schedule;

        _host.Downloads.ShouldNotContain(ExeUri("0.6.0"));
        File.ReadAllText(_installation.InstalledExe).ShouldBe("eski sürüm");
        await _connection.DidNotReceive().TryBeginUpdateAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>AC-1: yeni sürüm ilk açılışta API'ye bağlanınca güncelleme onaylanır; önceki sürüm saklı kalır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Startup_PendingConnected_Commits()
    {
        _installation.SaveState(new UpdateState(Current, 0, "0.4.0"));
        _connection.State.Returns(ConnectionState.Connected);
        var updater = Create();

        (await updater.StartupAsync(TestContext.Current.CancellationToken)).ShouldBe(UpdateStartup.Watching);

        await WaitUntilAsync(() => _installation.LoadState().PendingVersion is null, TestContext.Current.CancellationToken);
        _installation.LoadState().ShouldBe(new UpdateState(PreviousVersion: "0.4.0"));
        _install.Launches.ShouldBeEmpty();
        _logs.ShouldContain(e => e.Level == LogEventLevel.Information && e.MessageTemplate.Text.StartsWith("Güncelleme onaylandı", StringComparison.Ordinal));
    }

    /// <summary>
    /// AC-8: yeni sürüm 60 sn içinde bağlanamazsa önceki sürüme döner (önceki exe geri konur ve başlatılır, Error); önceki sürüm açılınca
    /// "geri dönüldü" bildirimi bir kez verilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Startup_PendingNoConnectionIn60s_RollsBackAndNotifies()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_installation.PreviousExe)!);
        File.WriteAllText(_installation.PreviousExe, "önceki çalışan");
        _installation.SaveState(new UpdateState(Current, 0, "0.4.0"));
        _connection.State.Returns(ConnectionState.Connecting);
        var updater = Create();
        var restarted = false;
        updater.RestartRequested += (_, _) => restarted = true;
        var ct = TestContext.Current.CancellationToken;

        (await updater.StartupAsync(ct)).ShouldBe(UpdateStartup.Watching);
        _time.Advance(TimeSpan.FromSeconds(59));
        await Task.Delay(50, ct);
        restarted.ShouldBeFalse();
        _time.Advance(TimeSpan.FromSeconds(1));
        await WaitUntilAsync(() => restarted, ct);

        File.ReadAllText(_installation.InstalledExe).ShouldBe("önceki çalışan");
        File.Exists(_installation.PreviousExe).ShouldBeFalse();
        _installation.LoadState().ShouldBe(new UpdateState(RolledBackFrom: Current));
        _install.Launches.ShouldBe([(_installation.InstalledExe, string.Empty)]);
        _logs.ShouldContain(e => e.Level == LogEventLevel.Error && e.MessageTemplate.Text.StartsWith("Geri dönüş", StringComparison.Ordinal));

        var previous = Create("0.4.0");
        (await previous.StartupAsync(ct)).ShouldBe(UpdateStartup.RolledBack);
        (await previous.StartupAsync(ct)).ShouldBe(UpdateStartup.Normal); // bildirim bir kez
    }

    /// <summary>AC-7: yeni sürüm önceki açılışında onaylanmadan kesildiyse (çökme/kapanma) bir sonraki açılışta hemen önceki sürüme dönülür.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Startup_PendingSecondLaunch_RollsBack()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_installation.PreviousExe)!);
        File.WriteAllText(_installation.PreviousExe, "önceki çalışan");
        _installation.SaveState(new UpdateState(Current, 1, "0.4.0"));

        (await Create().StartupAsync(TestContext.Current.CancellationToken)).ShouldBe(UpdateStartup.Restarting);

        File.ReadAllText(_installation.InstalledExe).ShouldBe("önceki çalışan");
        _install.Launches.ShouldBe([(_installation.InstalledExe, string.Empty)]);
    }

    /// <summary>AC-7: indirme yarıda kesilirse hiçbir şey değişmez; sonraki açılış yarım dosyayı siler ve eski sürümle açılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Apply_InterruptedDownload_NothingSwapped()
    {
        Publish("0.6.0", NewExe);
        var updater = Create();
        await updater.CheckAsync(TestContext.Current.CancellationToken);
        using var kill = new CancellationTokenSource();
        _host.BeforeDownload = (uri, destination) =>
        {
            File.WriteAllText(destination, "yarım");
            kill.Cancel(); // süreç zorla sonlandırıldı
        };

        await Should.ThrowAsync<OperationCanceledException>(() => updater.ApplyAsync(kill.Token));
        File.ReadAllText(_installation.InstalledExe).ShouldBe("eski sürüm");
        _installation.LoadState().ShouldBe(new UpdateState());

        (await Create().StartupAsync(TestContext.Current.CancellationToken)).ShouldBe(UpdateStartup.Normal);
        Directory.Exists(_installation.StagingDirectory).ShouldBeFalse();
        _install.Launches.ShouldBeEmpty();
    }

    /// <summary>AC-7: diskte yalnızca bir önceki sürüm saklanır (iki güncellemeden sonra en eski silinmiştir).</summary>
    [Fact]
    public void Apply_KeepsOnlyOnePrevious()
    {
        Directory.CreateDirectory(_installation.StagingDirectory);
        foreach (var version in new[] { "v2", "v3" })
        {
            var staged = Path.Combine(_installation.StagingDirectory, version + ".exe");
            File.WriteAllText(staged, version);
            _installation.Swap(staged);
        }

        File.ReadAllText(_installation.InstalledExe).ShouldBe("v3");
        File.ReadAllText(_installation.PreviousExe).ShouldBe("v2");
        Directory.GetFiles(Path.GetDirectoryName(_installation.PreviousExe)!).Length.ShouldBe(1);
        Directory.GetFiles(Path.GetDirectoryName(_installation.InstalledExe)!).Length.ShouldBe(1);
    }

    /// <summary>
    /// AC-13: denetim, yeni sürüm bulundu, başladı, bileşen güncellendi olayları Information; red Warning; doğrulama/geri dönüş Error (önceki
    /// testler); hiçbir log satırında imza, anahtar ya da belirteç geçmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Update_LogsEventsAtLevels_NoSecrets()
    {
        Publish("0.6.0", NewExe);
        var updater = Create();
        var ct = TestContext.Current.CancellationToken;
        await updater.CheckAsync(ct);
        await updater.ApplyAsync(ct);

        string[] information = ["Güncelleme denetimi", "Yeni sürüm bulundu", "Güncelleme başladı", "Bileşen güncellendi"];
        foreach (var start in information)
        {
            _logs.ShouldContain(e => e.Level == LogEventLevel.Information && e.MessageTemplate.Text.StartsWith(start, StringComparison.Ordinal), start);
        }

        var signature = Convert.ToBase64String(_host.Content[new Uri(Feed, "manifest.json.sig")]);
        foreach (var line in _logs.Select(e => e.RenderMessage()))
        {
            line.ShouldNotContain(_publicKey);
            line.ShouldNotContain(signature);
            line.ShouldNotContain(Convert.ToBase64String(_key.GetEncoded()));
        }
    }

    /// <summary>Review r1 M-2: aynı anda gelen denetimler sıraya girer; ikisi de listeyi doğrular (geçici dosya çakışması yok).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Check_Concurrent_BothVerify()
    {
        Publish("0.6.0", NewExe);
        var updater = Create();
        var ct = TestContext.Current.CancellationToken;

        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() => updater.CheckAsync(ct), ct)));

        results.ShouldAllBe(r => r == UpdateCheckOutcome.Available);
        _logs.ShouldNotContain(e => e.Level >= LogEventLevel.Warning);
    }

    /// <summary>İmza doğrulayıcı: anahtar yoksa ya da geçersizse yapılandırılmamış sayılır, hiçbir imzayı kabul etmez.</summary>
    [Fact]
    public void Verifier_WithoutKey_NotConfiguredRejectsAll()
    {
        var data = Encoding.UTF8.GetBytes("{}");
        new Ed25519ManifestVerifier(null).IsConfigured.ShouldBeFalse();
        new Ed25519ManifestVerifier("bozuk!").IsConfigured.ShouldBeFalse();
        new Ed25519ManifestVerifier(null).Verify(data, Sign(data, _key)).ShouldBeFalse();
        new Ed25519ManifestVerifier(_publicKey).Verify(data, Sign(data, _key)).ShouldBeTrue();
    }

    /// <summary>
    /// Yayın aracıyla uyum: <c>deploy/pi/agent-manifest.mjs sign</c> (Node yerleşik Ed25519) ile üretilmiş bir liste ajanın doğrulayıcısından geçer,
    /// tek bayt değişince geçmez. Anahtar yalnızca bu test için üretildi (gerçek yayın anahtarı değil).
    /// </summary>
    [Fact]
    public void Verifier_AcceptsNodeSignedManifest()
    {
        const string publicKey = "+BNGNef9DYy4voVPpIcO36r22SvWkgIzSLo3PGJqXS8=";
        var manifest = Encoding.UTF8.GetBytes(
            """{"version":"0.6.0","fileName":"anewconcept-ajan-0.6.0-win-x64.exe","sha256":"12d8938cf3f6fc515b06458818c2ae9ec2207959f8f008dbc2ddf2b86ddd34f2","size":8}""");
        var signature = Convert.FromBase64String("ZLRneVXli0Y6wKrF2CdcxJ7W6uuoZCBqHCEkzKLSXI4PC5qSxyq0tZq6P07gym5bYKdNm69o/+LPVZoyEgmlAQ==");
        var verifier = new Ed25519ManifestVerifier(publicKey);

        verifier.Verify(manifest, signature).ShouldBeTrue();
        manifest[12] ^= 1;
        verifier.Verify(manifest, signature).ShouldBeFalse();
    }

    private static (Ed25519PrivateKeyParameters Key, string Public) NewKey()
    {
        var generator = new Ed25519KeyPairGenerator();
        generator.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
        var pair = generator.GenerateKeyPair();
        return ((Ed25519PrivateKeyParameters)pair.Private, Convert.ToBase64String(((Ed25519PublicKeyParameters)pair.Public).GetEncoded()));
    }

    private static byte[] Sign(byte[] data, Ed25519PrivateKeyParameters key)
    {
        var signer = new Ed25519Signer();
        signer.Init(true, key);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }

    private static Uri ExeUri(string version) => new(Feed, $"anewconcept-ajan-{version}-win-x64.exe");

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 500 && !condition(); i++)
        {
            await Task.Delay(10, cancellationToken);
        }

        condition().ShouldBeTrue();
    }

    private void Publish(string version, byte[] exe)
    {
        var manifest = Encoding.UTF8.GetBytes(
            $$"""{"version":"{{version}}","fileName":"anewconcept-ajan-{{version}}-win-x64.exe","sha256":"{{FakeSetupHost.Sha(exe)}}","size":{{exe.Length}}}""");
        _host.Content[new Uri(Feed, "manifest.json")] = manifest;
        _host.Content[new Uri(Feed, "manifest.json.sig")] = Sign(manifest, _key);
        _host.Content[ExeUri(version)] = exe;
    }

    private AgentUpdater Create(string version = Current)
    {
        var updater = new AgentUpdater(version, Feed, _host, new Ed25519ManifestVerifier(_publicKey), _installation, _connection, _time, _logger);
        updater.Changed += (_, e) => _events.Add(e);
        return updater;
    }
}
