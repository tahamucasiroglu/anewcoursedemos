using System.Diagnostics;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Domain.Rules;
using Konsept.Agent.Infrastructure.Concrete;
using Konsept.Agent.Infrastructure.Sandbox;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using NSubstitute;
using Shouldly;

namespace Konsept.Agent.Tests;

/// <summary>
/// Ajanda kısıtlı render (spec 0018): Job Object sınırları (süre, süreç, bellek; geride süreç kalmaz), sıfırdan ortam, Node izinleri ve ağ
/// koruması bayrakları, iş klasöründe bağlantı reddi, render öncesi kapı, sahne işaretiyle devam, çıktı kapısı ve yazılım kodlayıcı.
/// Kasıtlı bozma: <c>RenderJobObject</c>'te ilgili sınır bayrağı ya da <c>SandboxRunner</c>'daki <c>Terminate</c> kaldırılınca sınır testleri,
/// <c>ProcessStageRunner</c>'daki işaret denetimi kaldırılınca devam testi, <c>RenderCodeGate.Check</c> çağrısı kaldırılınca kapı testi kırmızıya döner.
/// Gerçek HyperFrames render'ı ve honeypot gözlemi plan 0018 Build evidence'tadır (bileşen indirmesi gerekir).
/// </summary>
public sealed class RenderSandboxTests : IDisposable
{
    private static readonly Dictionary<string, string> CmdEnvironment = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SystemRoot"] = Environment.GetEnvironmentVariable("SystemRoot")!,
        ["ComSpec"] = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot")!, "System32", "cmd.exe"),
        ["PATH"] = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot")!, "System32"),
    };

    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("anewconcept-render-");

    /// <inheritdoc />
    public void Dispose() => _root.Delete(recursive: true);

    /// <summary>AC-3 (T9): sonsuz çalışan kod süre sınırında ağacıyla sonlandırılır; geride süreç kalmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Timeout_KillsWholeTree_NothingLeft()
    {
        var watch = Stopwatch.StartNew();
        var run = await SandboxRunner.RunAsync(
            Cmd("start /b ping -n 60 127.0.0.1 >nul & ping -n 60 127.0.0.1 >nul", new RenderLimits(TimeSpan.FromSeconds(2), 512L << 20, 16)),
            TestContext.Current.CancellationToken);

        run.LimitExceeded.ShouldBeTrue();
        run.RemainingProcesses.ShouldBe(0);
        watch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(30));
    }

    /// <summary>AC-3 (T9): yüzlerce alt süreç açmaya çalışan kod süreç sınırına takılır, sınır aşımı bildirilir; geride süreç kalmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ProcessFlood_HitsProcessLimit()
    {
        var run = await SandboxRunner.RunAsync(
            Cmd("for /L %i in (1,1,40) do start /b ping -n 3 127.0.0.1 >nul", new RenderLimits(TimeSpan.FromSeconds(20), 512L << 20, 4)),
            TestContext.Current.CancellationToken);

        run.LimitExceeded.ShouldBeTrue();
        run.RemainingProcesses.ShouldBe(0);
    }

    /// <summary>AC-3 (T9): aşırı bellek isteyen kod bellek sınırına takılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task MemoryHog_HitsMemoryLimit()
    {
        var system = Environment.GetEnvironmentVariable("SystemRoot")!;
        var env = new Dictionary<string, string>(CmdEnvironment, StringComparer.OrdinalIgnoreCase) { ["PSModulePath"] = Path.Combine(system, "System32", "WindowsPowerShell", "v1.0", "Modules") };
        var run = await SandboxRunner.RunAsync(
            new SandboxStart(
                Path.Combine(system, "System32", "WindowsPowerShell", "v1.0", "powershell.exe"),
                ["-NoProfile", "-NonInteractive", "-Command", "$ErrorActionPreference = 'Stop'; $a = New-Object byte[] (1500MB); $a[0] = 1; 'fin' + 'ished'"],
                _root.FullName,
                env,
                new RenderLimits(TimeSpan.FromSeconds(60), 400L << 20, 8)),
            TestContext.Current.CancellationToken);

        run.LimitExceeded.ShouldBeTrue();
        run.Output.ShouldNotContain("finished"); // Ayırma başarısız: komut sonuna ulaşmadı.
        run.RemainingProcesses.ShouldBe(0);
    }

    /// <summary>AC-5 (aşırı sıkılaştırma koruması): sınır içinde biten süreç normal sonuçlanır; ortam yalnızca verilen değişkenlerdir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Harmless_Completes_OnlyGivenEnvironment()
    {
        var run = await SandboxRunner.RunAsync(Cmd("set", new RenderLimits(TimeSpan.FromSeconds(20), 512L << 20, 8)), TestContext.Current.CancellationToken);

        run.LimitExceeded.ShouldBeFalse();
        run.ExitCode.ShouldBe(0);
        run.Output.ShouldNotContain("USERPROFILE", Case.Insensitive);
        run.Output.ShouldNotContain("APPDATA", Case.Insensitive);
    }

    /// <summary>
    /// AC-1 / AC-2 / AC-8: render ortamı sıfırdan — kullanıcı klasörleri iş klasörüne, proxy ölü adrese yönlü, telemetri kapalı, Chromium
    /// sandbox'ı zorunlu; Node yalnızca iş klasörüne yazar, kurulum kökünü (oturum belirteci) okuyamaz; ağ koruması ön yüklenir. Yönetici
    /// yetkisi isteyen hiçbir şey yok.
    /// </summary>
    [Fact]
    public void Environment_And_NodeArguments_Confined()
    {
        var install = Path.Combine(_root.FullName, "anewconcept");
        var job = Path.Combine(install, "jobs", "abc");
        var env = SandboxedSceneRenderer.Environment(install, job);
        foreach (var key in new[] { "TEMP", "TMP", "HOME", "USERPROFILE", "APPDATA", "LOCALAPPDATA", "HYPERFRAMES_CACHE_DIR" })
        {
            env[key].ShouldStartWith(job);
        }

        env["HTTP_PROXY"].ShouldBe(SandboxedSceneRenderer.DeadProxy);
        env["HTTPS_PROXY"].ShouldBe(SandboxedSceneRenderer.DeadProxy);
        env["NODE_USE_ENV_PROXY"].ShouldBe("1");
        env["HYPERFRAMES_NO_TELEMETRY"].ShouldBe("1");
        env["HYPERFRAMES_CHROME_SANDBOX"].ShouldBe("1");
        env.ContainsKey("USERNAME").ShouldBeFalse();

        var guard = Path.Combine(install, "sandbox", "render-guard.mjs");
        var args = SandboxedSceneRenderer.NodeArguments(install, guard, new SceneRenderRequest(job, Path.Combine(job, "scene-0"), 1080, 1920, 5, "clip.mp4"));
        args.ShouldContain("--permission");
        args.Where(a => a.StartsWith("--allow-fs-write=", StringComparison.Ordinal)).ShouldBe(["--allow-fs-write=" + job]);
        args.ShouldNotContain("--allow-fs-read=" + install);
        args.ShouldNotContain("--allow-fs-read=*");
        args[args.ToList().IndexOf("--import") + 1].ShouldBe(new Uri(guard).AbsoluteUri);
        args[args.ToList().IndexOf("--resolution") + 1].ShouldBe("portrait");
        args.ShouldNotContain("--format");
        SandboxedSceneRenderer.NodeArguments(install, guard, new SceneRenderRequest(job, job, 1920, 1080, 5, "clip.webm"))
            .ShouldContain("webm");
    }

    /// <summary>AC-1 (T1–T2): gömülü ağ koruması ajanın Chromium'una ölü proxy, DNS kapatma ve WebRTC UDP kapatma bayraklarını ekler.</summary>
    [Fact]
    public void Guard_Embedded_WithNetworkFlags()
    {
        using var stream = typeof(SandboxedSceneRenderer).Assembly.GetManifestResourceStream(SandboxedSceneRenderer.GuardResource);
        stream.ShouldNotBeNull();
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        text.ShouldContain("--proxy-server=http://127.0.0.1:9");
        text.ShouldContain("--host-resolver-rules=MAP * ~NOTFOUND , EXCLUDE localhost");
        text.ShouldContain("--force-webrtc-ip-handling-policy=disable_non_proxied_udp");
        text.ShouldContain("PRODUCER_HEADLESS_SHELL_PATH");
    }

    /// <summary>AC-2 (T4): iş klasörü içinden dışarıyı gösteren bağlantı (junction) varsa render yapılmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task JobFolderLink_Unsafe_NoRender()
    {
        var job = Directory.CreateDirectory(Path.Combine(_root.FullName, "job"));
        var outside = Directory.CreateDirectory(Path.Combine(_root.FullName, "outside"));
        Konsept.Agent.Infrastructure.Base.SceneRendererBase.HasReparsePoint(job.FullName).ShouldBeFalse();
        using (var mklink = Process.Start(new ProcessStartInfo("cmd.exe", ["/c", "mklink", "/J", Path.Combine(job.FullName, "link"), outside.FullName]) { CreateNoWindow = true, UseShellExecute = false })!)
        {
            await mklink.WaitForExitAsync(TestContext.Current.CancellationToken);
        }

        Konsept.Agent.Infrastructure.Base.SceneRendererBase.HasReparsePoint(job.FullName).ShouldBeTrue();
        var renderer = new SandboxedSceneRenderer(Path.Combine(_root.FullName, "install"), new RenderLimits(TimeSpan.FromSeconds(5), 1L << 30, 8), Serilog.Core.Logger.None);
        var result = await renderer.RenderAsync(new SceneRenderRequest(job.FullName, job.FullName, 1920, 1080, 5, "clip.mp4"), TestContext.Current.CancellationToken);
        result.Outcome.ShouldBe(SceneRenderOutcome.Unsafe);
        Directory.Delete(Path.Combine(job.FullName, "link"));
    }

    /// <summary>
    /// AC-4 (ajan tarafı): yasaklı yapı içeren kod render edilmez (<c>code_rejected</c>, sahne + neden); AC-7: 5 sahneli işte 3. sahnenin render'ı
    /// sırasında Durdur → devamda 1. ve 2. sahne yeniden render edilmez, yalnızca 3–5; düzeltilen sahne (kod değişti) yeniden render edilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Runner_GateRejects_AndResumeSkipsFinishedScenes()
    {
        var store = Substitute.For<ISetupStateStore>();
        store.Load().Returns(new SetupState(
            new Dictionary<string, InstalledComponent> { ["node"] = new("1", "x"), ["ffmpeg"] = new("1", "x"), ["chromium"] = new("1", "x"), ["hyperframes"] = new("1", "x") },
            null));
        var renderer = new FakeRenderer();
        var runner = new ProcessStageRunner(store, _root.FullName, () => _root.FullName, renderer, Serilog.Core.Logger.None);
        var job = Guid.NewGuid();
        var codes = Enumerable.Range(0, 5).Select(i => $"<!doctype html><html><body><div>sahne {i}</div></body></html>").ToArray();
        AgentStageCommand Command() => new()
        {
            JobId = job,
            WorkKey = job,
            Stage = AgentStageCommand.Render,
            Engine = "local/hyperframes",
            Language = "tr",
            Format = "portrait",
            Scenes = [.. codes.Select((c, i) => new AgentStageScene(i, 5, "anlatım", null, c, null))],
        };

        var bad = Command() with { Scenes = [new AgentStageScene(0, 5, "x", null, "<html><script>fetch('/x')</script></html>", null)] };
        var rejected = await runner.RunAsync(bad);
        (rejected.Error, rejected.FailedScene, rejected.Detail).ShouldBe((AgentStageReply.CodeRejected, 0, "forbidden: fetch"));
        renderer.Calls.ShouldBeEmpty();

        renderer.OnRender = index =>
        {
            if (index == 2)
            {
                runner.Cancel(job); // Durdur 3. sahnenin render'ı sırasında.
            }
        };
        (await runner.RunAsync(Command())).Error.ShouldBe(AgentStageReply.Canceled);
        renderer.Calls.ShouldBe([0, 1, 2]);

        renderer.OnRender = null;
        renderer.Calls.Clear();
        var resumed = await runner.RunAsync(Command());
        resumed.Ok.ShouldBeTrue();
        renderer.Calls.ShouldBe([2, 3, 4]);
        renderer.Requests.ShouldAllBe(r => r.Width == 1080 && r.Height == 1920); // AC-6: konseptin kaydındaki biçim (dikey).

        renderer.Calls.Clear();
        codes[1] = codes[1].Replace("sahne 1", "sahne 1 düzeltildi", StringComparison.Ordinal);
        (await runner.RunAsync(Command())).Ok.ShouldBeTrue();
        renderer.Calls.ShouldBe([1]);
    }

    /// <summary>AC-5 (L7): çıktı kapısı — oynatılamayan dosya, farklı boyut ya da süre reddedilir; beklenen klip geçer.</summary>
    [Fact]
    public void OutputGate_ChecksPlayableSizeDuration()
    {
        const string Ok = """{"streams":[{"codec_type":"video","width":1080,"height":1920}],"format":{"duration":"5.033"}}""";
        OutputGate.Problem(Ok, 1080, 1920, 5).ShouldBeNull();
        OutputGate.Problem(null, 1080, 1920, 5).ShouldBe("not a playable video");
        OutputGate.Problem("garbage", 1080, 1920, 5).ShouldBe("not a playable video");
        OutputGate.Problem("""{"streams":[{"codec_type":"audio"}],"format":{"duration":"5"}}""", 1080, 1920, 5).ShouldBe("no video stream");
        OutputGate.Problem(Ok, 1920, 1080, 5).ShouldBe("unexpected size");
        OutputGate.Problem(Ok, 1080, 1920, 30).ShouldBe("unexpected duration");
    }

    /// <summary>B-117: NVENC yoksa birleştirme yazılım kodlayıcıyla (libopenh264, bit hızıyla); NVENC varsa 0017 kuralı (CPU'ya düşmeden hata).</summary>
    [Fact]
    public void Merge_SoftwareEncoder_WhenNoNvenc()
    {
        var args = MergeTemplate.Arguments("c.txt", null, "f", 1, "o.mp4", MergeTemplate.SoftwareEncoder).ToList();
        args[args.IndexOf("-c:v") + 1].ShouldBe("libopenh264");
        args[args.IndexOf("-b:v") + 1].ShouldBe(MergeTemplate.SoftwareBitrate);
        MergeTemplate.NvencProbeArguments.ShouldContain(MergeTemplate.Encoder);
        MergeTemplate.Arguments("c.txt", null, "f", 1, "o.mp4").ShouldNotContain("-b:v");
    }

    private SandboxStart Cmd(string command, RenderLimits limits) =>
        new(CmdEnvironment["ComSpec"], ["/d", "/c", command], _root.FullName, CmdEnvironment, limits);

    private sealed class FakeRenderer : ISceneRenderer
    {
        public List<int> Calls { get; } = [];

        public List<SceneRenderRequest> Requests { get; } = [];

        public Action<int>? OnRender { get; set; }

        public async Task<SceneRenderResult> RenderAsync(SceneRenderRequest request, CancellationToken cancellationToken)
        {
            var index = int.Parse(Path.GetFileName(request.SceneDirectory)["scene-".Length..], System.Globalization.CultureInfo.InvariantCulture);
            Calls.Add(index);
            Requests.Add(request);
            OnRender?.Invoke(index);
            cancellationToken.ThrowIfCancellationRequested();
            await File.WriteAllTextAsync(Path.Combine(request.SceneDirectory, request.ClipName), "klip", cancellationToken);
            return new SceneRenderResult(SceneRenderOutcome.Ok);
        }
    }
}
