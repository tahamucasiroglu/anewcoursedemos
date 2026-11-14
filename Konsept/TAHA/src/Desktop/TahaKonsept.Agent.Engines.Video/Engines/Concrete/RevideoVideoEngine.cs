using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Abstractions.Video;
using TahaKonsept.Agent.Engines.Video.Engines.Base;
using TahaKonsept.Agent.Engines.Video.Fonts;
using TahaKonsept.Agent.Engines.Video.Media.Abstract;
using TahaKonsept.Agent.Engines.Video.Safety.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Engines.Concrete;

/// <summary>
/// Revideo (<c>@revideo/*@0.11.0</c>, MIT, Motion Canvas türevi; deneysel) video motoru. LLM <c>src/scene.tsx</c> yazar
/// (<c>export default makeScene2D('scene', function* (view) {...})</c>); motor <c>src/project.ts</c> (boyut/fps istekten),
/// <c>src/tk-fonts.css</c> ve <c>render.mjs</c> (renderVideo API) üretir. Proje şablon içinde <c>work/&lt;guid&gt;</c> altında kurulur.
/// Notlar (yerelde doğrulandı): Revideo Chrome'a her zaman <c>--single-process</c> ekler ve yeni Chrome'da sayfa düşer — render.mjs
/// bunu engeller; sistem ffmpeg 8 ile <c>lavfi</c> algılaması bozuk olduğundan Revideo kendi paketli ffmpeg'ini kullanır
/// (<c>FFMPEG_PATH</c> verilmez). İlerleme: <c>@tk-progress N</c>.
/// </summary>
public sealed partial class RevideoVideoEngine : TsxVideoEngineBase
{
    /// <summary>Motor anahtarı.</summary>
    public const string EngineKey = "revideo";

    /// <summary>Sabit Revideo sürümü.</summary>
    public const string RevideoVersion = "0.11.0";

    /// <summary>Sabit @puppeteer/browsers sürümü (Chrome indirme).</summary>
    public const string PuppeteerBrowsersVersion = "3.2.7";

    private const string BrowsersFolder = ".browsers";

    private static readonly IReadOnlySet<string> Imports = new HashSet<string>(StringComparer.Ordinal) { "@revideo/2d", "@revideo/core" };

    /// <summary>Motor oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="tools">Araç bulucu.</param>
    /// <param name="runner">Süreç çalıştırıcı.</param>
    /// <param name="probe">Medya süresi okuyucu.</param>
    /// <param name="guard">Güvenlik denetçisi.</param>
    /// <param name="logger">Günlükçü.</param>
    public RevideoVideoEngine(IAgentPaths paths, IToolLocator tools, IProcessRunner runner, IMediaProbe probe, ISceneCodeGuard guard, ILogger<RevideoVideoEngine> logger)
        : base(paths, tools, runner, probe, guard, logger)
    {
    }

    /// <inheritdoc />
    public override string Key => EngineKey;

    /// <inheritdoc />
    public override string DisplayName => "Revideo (deneysel)";

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> Dependencies { get; } = BuildDependencies();

    /// <inheritdoc />
    protected override string CliRelativePath => "node_modules/@revideo/renderer/package.json";

    /// <inheritdoc />
    protected override bool UsesEsModules => true;

    /// <inheritdoc />
    protected override IReadOnlySet<string> AllowedImports => Imports;

    /// <inheritdoc />
    public override double? ParseProgress(string line)
    {
        var match = ProgressRegex().Match(line);
        return match.Success ? double.Parse(match.Groups["p"].Value, CultureInfo.InvariantCulture) : null;
    }

    /// <inheritdoc />
    protected override bool IsRuntimeReady() => ResolveChrome() is not null;

    /// <inheritdoc />
    protected override async Task<Result> InstallRuntimeAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        if (ResolveChrome() is not null)
        {
            return Result.Success();
        }

        progress?.Report(new EngineSetupProgress("Revideo: headless Chrome indiriliyor…", 80));
        var cli = Path.Combine(TemplateDir, "node_modules", "@puppeteer", "browsers", "lib", "main-cli.js");
        var install = await RunNodeAsync([cli, "install", "chrome-headless-shell@stable", "--path", Path.Combine(TemplateDir, BrowsersFolder)], TimeSpan.FromMinutes(15), ct).ConfigureAwait(false);
        if (install.IsFailure)
        {
            return install.Error;
        }

        return install.Value.Succeeded && ResolveChrome() is not null
            ? Result.Success()
            : Error.Failure("video.revideo.browser_failed", $"Revideo için Chrome indirilemedi: {Tail(install.Value.StandardError, install.Value.StandardOutput)}");
    }

    /// <inheritdoc />
    protected override Dictionary<string, string> BuildEnvironment()
    {
        var env = base.BuildEnvironment();
        env.Remove("FFMPEG_PATH");
        env.Remove("FFPROBE_PATH");
        env["DISABLE_TELEMETRY"] = "true";
        return env;
    }

    /// <inheritdoc />
    protected override async Task<Result<SceneProject>> PrepareProjectAsync(SceneRenderRequest request, CancellationToken ct)
    {
        var chrome = ResolveChrome();
        if (chrome is null)
        {
            return Error.Failure("video.revideo.browser_missing", "Revideo için headless Chrome bulunamadı; motoru yeniden kurun.");
        }

        var codeCopy = Path.Combine(request.WorkDir, string.Create(CultureInfo.InvariantCulture, $"revideo-scene-{request.SceneIndex:D3}.tsx"));
        await File.WriteAllTextAsync(codeCopy, request.SceneCode, Utf8NoBom, ct).ConfigureAwait(false);

        var dir = CreateTemplateWorkDir();
        var src = Path.Combine(dir, "src");
        var publicDir = Path.Combine(dir, "public");
        Directory.CreateDirectory(src);
        CopyBundledFonts(Path.Combine(publicDir, BundledFontCatalog.FontsFolder));
        await File.WriteAllTextAsync(Path.Combine(src, "tk-fonts.css"), BundledFontCatalog.BuildCss("/fonts/"), Utf8NoBom, ct).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(src, "scene.tsx"), request.SceneCode, Utf8NoBom, ct).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(src, "project.ts"), BuildProject(request.Width, request.Height, request.Fps), Utf8NoBom, ct).ConfigureAwait(false);
        var entry = Path.Combine(dir, "render.mjs");
        await File.WriteAllTextAsync(entry, RenderScript, Utf8NoBom, ct).ConfigureAwait(false);
        return new SceneProject(dir, Path.Combine(publicDir, "assets"), entry, DeleteAfterRender: true);
    }

    /// <summary>
    /// Chrome GPU bayrakları ("Önce GPU"; render.mjs bunları ağ yasağı bayraklarına ekler, ağ yasağı her iki kipte de aynen kalır).
    /// GPU: <c>--enable-gpu</c> + GPU rasterleştirme + GPU kara listesini yok sayma. <c>chrome-headless-shell</c> GPU'yu kendiliğinden
    /// açmaz: <c>--enable-gpu</c> olmadan rasterleştirme/birleştirme <c>disabled_software</c>, WebGL SwiftShader kalır (ölçüm, SystemInfo);
    /// bu bayrakla ANGLE/D3D11 + RTX (<c>enabled_force</c>). <c>--use-gl</c>/<c>--use-angle</c> bayrakları Revideo'da sessiz ses dosyası
    /// süresini bozuyor ("Invalid duration specification for t: N/A"), bu yüzden eklenmez. GPU açıkken Chrome WebCodecs'te donanım
    /// kodlayıcıyı seçer ve Revideo'nun mp4-wasm çıktısının süresi okunamaz (aynı "t: N/A" hatası); <c>--disable-accelerated-video-encode</c>
    /// yalnız bu ara kodlamayı yazılımda tutar (çizim GPU'da kalır). CPU yedeği: <c>--disable-gpu</c>. Kodlama Revideo'nun kendi ffmpeg'inde
    /// sabit yazılım kodlayıcıyla yapılır (0.11 API'si kodlayıcı seçtirmez); ara dosya birleştiricide NVENC ile yeniden kodlanır.
    /// </summary>
    /// <param name="useGpu">GPU açık mı.</param>
    /// <returns>Chrome bayrakları.</returns>
    public static IReadOnlyList<string> ChromeGpuArguments(bool useGpu) => useGpu
        ? ["--enable-gpu", "--enable-gpu-rasterization", "--ignore-gpu-blocklist", "--disable-accelerated-video-encode"]
        : ["--disable-gpu"];

    /// <inheritdoc />
    protected override Result<IReadOnlyList<string>> BuildRenderArguments(SceneProject project, SceneRenderRequest request, bool useGpu)
    {
        var chrome = ResolveChrome();
        return chrome is null
            ? Error.Failure("video.revideo.browser_missing", "Revideo için headless Chrome bulunamadı.")
            : new List<string>
            {
                project.EntryPath, Path.Combine(project.ProjectDir, "out"), "scene.mp4", "-", chrome,
                JsonSerializer.Serialize(ChromeGpuArguments(useGpu)),
            };
    }

    /// <inheritdoc />
    protected override Task<Result> FinalizeOutputAsync(SceneProject project, SceneRenderRequest request, CancellationToken ct)
    {
        var produced = Path.Combine(project.ProjectDir, "out", "scene.mp4");
        if (!File.Exists(produced))
        {
            return Task.FromResult(Result.Failure(Error.Failure("video.revideo.no_output", "Revideo çıktı dosyası üretmedi.")));
        }

        File.Move(produced, request.OutputPath, overwrite: true);
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Revideo proje modülü (<c>src/project.ts</c>). Paketlenen yazı tiplerinin tüm ağırlıkları sahne kurulmadan önce yüklenir
    /// (üst düzey <c>await document.fonts.load</c>): aksi halde düzen (layout) yedek yazı tipinin ölçüleriyle hesaplanıp önbelleğe
    /// alınıyor, yazı tipi sonradan gelince kart içindeki kaydırılmış ikinci satır kartın dışına taşıyordu.
    /// </summary>
    /// <param name="width">Genişlik.</param>
    /// <param name="height">Yükseklik.</param>
    /// <param name="fps">Kare hızı.</param>
    /// <returns>TypeScript kaynak metni.</returns>
    public static string BuildProject(int width, int height, int fps) =>
        string.Create(CultureInfo.InvariantCulture, $$"""
            import { makeProject } from '@revideo/core';
            import './tk-fonts.css';
            import scene from './scene?scene';

            const tkFaces: string[] = [{{BuildFontLoadList()}}];
            await Promise.all(tkFaces.map((face) => document.fonts.load(face, 'AaĞğİıŞşÇçÖöÜüÂâ').catch(() => [])));
            await document.fonts.ready;

            export default makeProject({
              scenes: [scene],
              settings: { shared: { size: { x: {{width}}, y: {{height}} }, background: '#000000' }, rendering: { fps: {{fps}} } },
            });

            """);

    private static string BuildFontLoadList() =>
        string.Join(", ", BundledFontCatalog.Fonts.SelectMany(f => f.Weights.Select(w =>
            string.Create(CultureInfo.InvariantCulture, $"'{w} 64px \"{f.Family}\"'"))));

    private const string RenderScript = """
        import { renderVideo } from '@revideo/renderer';

        const [, , outDir, outFile, ffmpegPath, chromePath, gpuFlagsJson] = process.argv;
        // Revideo her zaman --single-process ekler; yeni Chrome sürümlerinde sayfa düşüyor. includes() true dönen dizi bunu engeller.
        class NoSingleProcessArgs extends Array { includes() { return true; } }
        // LLM'in yazdığı sahne kodu güvenilmez: Chromium sandbox'ı açık kalır (--no-sandbox yok) ve ağ kapalıdır — tüm istekler ölü proxy'ye
        // (127.0.0.1:9) gider, yalnız loopback (Vite paket sunucusu) varsayılan olarak proxy dışıdır; WebRTC UDP'si de proxy dışına çıkamaz.
        const network = ['--proxy-server=http://127.0.0.1:9', '--force-webrtc-ip-handling-policy=disable_non_proxied_udp'];
        // "Önce GPU": ajan GPU (ANGLE/D3D11) ya da CPU yedeği (SwiftShader) bayraklarını JSON dizi olarak verir; ağ yasağı her iki kipte de aynı.
        const gpu = gpuFlagsJson ? JSON.parse(gpuFlagsJson) : [];
        const args = NoSingleProcessArgs.from([...network, ...gpu, ...(process.platform === 'linux' ? ['--disable-dev-shm-usage'] : [])]);
        let last = -1;
        await renderVideo({
          projectFile: './src/project.ts',
          settings: {
            outDir, outFile, workers: 1, logProgress: false,
            ...(ffmpegPath && ffmpegPath !== '-' ? { ffmpeg: { ffmpegPath } } : {}),
            puppeteer: { executablePath: chromePath, headless: 'shell', args },
            viteConfig: { publicDir: 'public', logLevel: 'warn' },
            progressCallback: (_w, p) => { const pct = Math.floor(p * 100); if (pct !== last) { last = pct; console.log(`@tk-progress ${pct}`); } },
          },
        });
        console.log('@tk-done');

        """;

    private static Dictionary<string, string> BuildDependencies() => new(StringComparer.Ordinal)
    {
        ["@revideo/core"] = RevideoVersion,
        ["@revideo/2d"] = RevideoVersion,
        ["@revideo/renderer"] = RevideoVersion,
        ["@revideo/vite-plugin"] = RevideoVersion,
        ["@revideo/ffmpeg"] = RevideoVersion,
        ["@revideo/ui"] = RevideoVersion,
        ["@puppeteer/browsers"] = PuppeteerBrowsersVersion,
        ["@fontsource/inter"] = BundledFontCatalog.PackageVersion,
        ["@fontsource/montserrat"] = BundledFontCatalog.PackageVersion,
        ["@fontsource/playfair-display"] = BundledFontCatalog.PackageVersion,
    };

    private string? ResolveChrome()
    {
        if (Tools.Find(ToolKind.ChromeHeadlessShell) is { } chrome && File.Exists(chrome))
        {
            return chrome;
        }

        var exe = OperatingSystem.IsWindows() ? "chrome-headless-shell.exe" : "chrome-headless-shell";
        return FindInTemplate(BrowsersFolder, exe);
    }

    [GeneratedRegex(@"^@tk-progress\s+(?<p>\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex ProgressRegex();
}
