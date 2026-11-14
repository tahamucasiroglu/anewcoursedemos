using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Engines.Video.Engines.Concrete;
using TahaKonsept.Agent.Engines.Video.Media.Concrete;
using TahaKonsept.Agent.Engines.Video.Safety.Concrete;
using TahaKonsept.Agent.Engines.Video.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Video.Tests.Integration;

/// <summary>
/// Gerçek Chrome: Revideo'nun paketli <c>chrome-headless-shell</c>'i <see cref="RevideoVideoEngine.ChromeGpuArguments"/> ve ağ yasağı
/// bayraklarıyla açılır, CDP <c>SystemInfo.getInfo</c> ile rasterleştirme durumu okunur. GPU kipinde donanım rasterleştirme
/// (<c>enabled*</c>) beklenir — eskiden <c>--enable-gpu</c> olmadığı için <c>disabled_software</c> (SwiftShader, CPU) kalıyordu.
/// CPU yedeğinde yazılım beklenir. NVIDIA GPU (nvidia-smi) yoksa ya da motor testleri kapalıysa atlanır.
/// </summary>
public sealed class RevideoChromeGpuTests
{
    private const string ProbeScript = """
        import { pathToFileURL } from 'node:url';
        import { createRequire } from 'node:module';
        import path from 'node:path';
        const [, , templateDir, chromePath, flagsJson] = process.argv;
        const entry = createRequire(path.join(templateDir, 'package.json')).resolve('puppeteer-core');
        const { default: puppeteer } = await import(pathToFileURL(entry).href);
        const args = ['--proxy-server=http://127.0.0.1:9', ...JSON.parse(flagsJson)];
        const browser = await puppeteer.launch({ executablePath: chromePath, headless: 'shell', args });
        try {
          const cdp = await browser.target().createCDPSession();
          const info = await cdp.send('SystemInfo.getInfo');
          console.log('@tk-gpu ' + JSON.stringify(info.gpu.featureStatus));
        } finally {
          await browser.close();
        }
        """;

    /// <summary>GPU bayrakları donanım rasterleştirmeyi açar; CPU yedeği yazılımda kalır.</summary>
    [Fact]
    public async Task Revideo_chrome_gpu_flags_enable_hardware_rasterization()
    {
        Assert.SkipUnless(TestEnvironment.CanRunEngines, "node/ffmpeg/.tools-dev yok ya da motor testleri kapalı.");
        Assert.SkipUnless(File.Exists(Path.Combine(Environment.SystemDirectory, "nvidia-smi.exe")), "NVIDIA GPU yok.");
        var ct = TestContext.Current.CancellationToken;
        var tools = new TestToolLocator();
        var runner = new TestProcessRunner();
        var engine = new RevideoVideoEngine(
            new TestAgentPaths(TestEnvironment.NewTempDir("agent"), TestEnvironment.EngineToolsDir), tools, runner,
            new FfprobeMediaProbe(tools, runner), new DefaultSceneCodeGuard(), NullLogger<RevideoVideoEngine>.Instance);
        (await engine.EnsureReadyAsync(null, ct)).IsSuccess.ShouldBeTrue();
        var templateDir = Path.Combine(TestEnvironment.EngineToolsDir, "engines", "revideo");
        var chrome = Directory.EnumerateFiles(Path.Combine(templateDir, ".browsers"), "chrome-headless-shell.exe", SearchOption.AllDirectories).First();
        var script = Path.Combine(TestEnvironment.NewTempDir("revideo-gpu"), "probe.mjs");
        await File.WriteAllTextAsync(script, ProbeScript, ct);

        var gpu = await ReadFeatureStatusAsync(tools, runner, script, templateDir, chrome, RevideoVideoEngine.ChromeGpuArguments(true), ct);
        var cpu = await ReadFeatureStatusAsync(tools, runner, script, templateDir, chrome, RevideoVideoEngine.ChromeGpuArguments(false), ct);

        gpu["rasterization"].ShouldStartWith("enabled");
        gpu["gpu_compositing"].ShouldStartWith("enabled");
        cpu["rasterization"].ShouldNotStartWith("enabled");
    }

    private static async Task<Dictionary<string, string>> ReadFeatureStatusAsync(
        TestToolLocator tools, TestProcessRunner runner, string script, string templateDir, string chrome, IReadOnlyList<string> flags, CancellationToken ct)
    {
        var node = tools.Find(ToolKind.Node)!;
        var result = await runner.RunAsync(
            new ProcessSpec(node, [script, templateDir, chrome, JsonSerializer.Serialize(flags)], templateDir, null, TimeSpan.FromMinutes(2)), null, ct);
        result.Succeeded.ShouldBeTrue(result.StandardError);
        var line = result.StandardOutput.Split('\n').Single(l => l.StartsWith("@tk-gpu ", StringComparison.Ordinal));
        return JsonSerializer.Deserialize<Dictionary<string, string>>(line["@tk-gpu ".Length..].Trim())!;
    }
}
