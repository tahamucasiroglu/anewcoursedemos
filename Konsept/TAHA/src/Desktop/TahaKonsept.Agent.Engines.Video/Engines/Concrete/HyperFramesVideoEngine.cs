using System.Globalization;
using System.Text;
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
/// HyperFrames (<c>hyperframes@0.9.43</c>, HTML + CSS + GSAP) video motoru — varsayılan motor.
/// Sahne projesi: <c>WorkDir/hyperframes-scene-NNN/</c> altında <c>index.html</c> (LLM kodu), <c>gsap.min.js</c>,
/// <c>fonts/</c> (+ <c>fonts/tk-fonts.css</c>) ve <c>assets/</c>. Render: <c>hyperframes render &lt;dir&gt; -o out.mp4 --fps N --strict</c>;
/// ilerleme <c>@hf-progress {json}</c> satırlarından okunur. Doğrulama: <c>hyperframes lint --json</c> (sahne görselleri doğrulama klasörünün <c>assets/</c>'ına da kopyalanır).
/// Kök öğenin <c>data-width/data-height</c> değerleri istenen boyuta zorlanır. Telemetri <c>HYPERFRAMES_NO_TELEMETRY</c> ile kapalı.
/// </summary>
public sealed partial class HyperFramesVideoEngine : NodeVideoEngineBase
{
    /// <summary>Motor anahtarı.</summary>
    public const string EngineKey = "hyperframes";

    /// <summary>Sabit HyperFrames sürümü.</summary>
    public const string HyperFramesVersion = "0.9.43";

    /// <summary>Sabit GSAP sürümü (yerel kopya; CDN yok).</summary>
    public const string GsapVersion = "3.15.1";

    /// <summary>Motor oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="tools">Araç bulucu.</param>
    /// <param name="runner">Süreç çalıştırıcı.</param>
    /// <param name="probe">Medya süresi okuyucu.</param>
    /// <param name="guard">Güvenlik denetçisi.</param>
    /// <param name="logger">Günlükçü.</param>
    public HyperFramesVideoEngine(IAgentPaths paths, IToolLocator tools, IProcessRunner runner, IMediaProbe probe, ISceneCodeGuard guard, ILogger<HyperFramesVideoEngine> logger)
        : base(paths, tools, runner, probe, guard, logger)
    {
    }

    /// <inheritdoc />
    public override string Key => EngineKey;

    /// <inheritdoc />
    public override string DisplayName => "HyperFrames";

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> Dependencies { get; } = BuildDependencies();

    /// <inheritdoc />
    protected override string CliRelativePath => "node_modules/hyperframes/bin/hyperframes.mjs";

    private string CacheDir => Path.Combine(TemplateDir, ".hf-cache");

    /// <inheritdoc />
    public override double? ParseProgress(string line)
    {
        const string marker = "@hf-progress ";
        var index = line.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(line[(index + marker.Length)..]);
            return doc.RootElement.TryGetProperty("pct", out var pct) && pct.TryGetDouble(out var value) ? value : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Kök kompozisyon öğesinin <c>data-width</c>/<c>data-height</c> değerlerini istenen boyuta zorlar (yoksa ekler).</summary>
    /// <param name="html">Sahne HTML'i.</param>
    /// <param name="width">Genişlik.</param>
    /// <param name="height">Yükseklik.</param>
    /// <returns>Düzeltilmiş HTML.</returns>
    public static string ForceRootSize(string html, int width, int height)
    {
        var match = RootTagRegex().Match(html);
        if (!match.Success)
        {
            return html;
        }

        var tag = match.Value;
        tag = SetAttribute(tag, "data-width", width.ToString(CultureInfo.InvariantCulture));
        tag = SetAttribute(tag, "data-height", height.ToString(CultureInfo.InvariantCulture));
        return string.Concat(html.AsSpan(0, match.Index), tag, html.AsSpan(match.Index + match.Length));
    }

    /// <inheritdoc />
    protected override Result ValidateStructure(string sceneCode)
    {
        if (!RootTagRegex().IsMatch(sceneCode))
        {
            return Error.Validation("video.hyperframes.no_root", "Kök öğede data-composition-id yok (ör. <div id=\"root\" data-composition-id=\"main\" ...>).");
        }

        return sceneCode.Contains("__timelines", StringComparison.Ordinal)
            ? Result.Success()
            : Error.Validation("video.hyperframes.no_timeline", "window.__timelines[\"<kompozisyon-id>\"] kaydı yok.");
    }

    /// <inheritdoc />
    protected override async Task<Result> ValidateWithToolAsync(SceneRenderRequest request, CancellationToken ct)
    {
        var dir = Path.Combine(request.WorkDir, string.Create(CultureInfo.InvariantCulture, $"hyperframes-validate-{request.SceneIndex:D3}"));
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "index.html"), ForceRootSize(request.SceneCode, request.Width, request.Height), Utf8NoBom, ct).ConfigureAwait(false);

        // lint, <img src="assets/x.png"> başvurularını diskte arar (missing_local_asset); görseller doğrulama klasörüne de konur.
        CopyAssets(request.AssetPaths, Path.Combine(dir, "assets"));
        var run = await RunNodeAsync([CliPath, "lint", dir, "--json"], TimeSpan.FromMinutes(2), ct).ConfigureAwait(false);
        if (run.IsFailure)
        {
            return run.Error;
        }

        var report = ParseLintReport(run.Value.StandardOutput);
        if (report is null)
        {
            return run.Value.Succeeded
                ? Result.Success()
                : Error.Failure("video.hyperframes.lint_failed", $"hyperframes lint çalışmadı: {Tail(run.Value.StandardError, run.Value.StandardOutput)}");
        }

        return report.Length == 0
            ? Result.Success()
            : Error.Validation("video.hyperframes.lint", "HyperFrames lint hataları:\n" + report);
    }

    /// <summary>
    /// <c>lint --json</c> çıktısını okur; yalnızca <c>error</c> düzeyindeki bulguları (kod, mesaj, öneri) satır satır döner.
    /// JSON okunamazsa <c>null</c>.
    /// </summary>
    /// <param name="stdout">lint çıktısı.</param>
    /// <returns>Hata metni (boşsa hata yok) ya da <c>null</c>.</returns>
    public static string? ParseLintReport(string stdout)
    {
        var start = stdout.IndexOf('{', StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(stdout[start..]);
            if (!doc.RootElement.TryGetProperty("findings", out var findings) || findings.ValueKind != JsonValueKind.Array)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var finding in findings.EnumerateArray())
            {
                if (!string.Equals(GetString(finding, "severity"), "error", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                sb.Append("- [").Append(GetString(finding, "code")).Append("] ").Append(GetString(finding, "message"));
                var hint = GetString(finding, "fixHint");
                if (hint.Length > 0)
                {
                    sb.Append(" Öneri: ").Append(hint);
                }

                sb.Append('\n');
            }

            return sb.ToString().TrimEnd();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override bool IsRuntimeReady() => ResolveChrome() is not null;

    /// <inheritdoc />
    protected override async Task<Result> InstallRuntimeAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        _ = await RunNodeAsync([CliPath, "telemetry", "disable"], TimeSpan.FromMinutes(1), ct).ConfigureAwait(false);
        if (ResolveChrome() is not null)
        {
            return Result.Success();
        }

        progress?.Report(new EngineSetupProgress("HyperFrames: headless Chrome indiriliyor…", 80));
        var ensure = await RunNodeAsync([CliPath, "browser", "ensure"], TimeSpan.FromMinutes(15), ct).ConfigureAwait(false);
        if (ensure.IsFailure)
        {
            return ensure.Error;
        }

        return ensure.Value.Succeeded && ResolveChrome() is not null
            ? Result.Success()
            : Error.Failure("video.hyperframes.browser_failed", $"HyperFrames için Chrome indirilemedi: {Tail(ensure.Value.StandardError, ensure.Value.StandardOutput)}");
    }

    /// <inheritdoc />
    protected override Dictionary<string, string> BuildEnvironment()
    {
        var env = base.BuildEnvironment();
        env["HYPERFRAMES_NO_TELEMETRY"] = "1";

        // Tarayıcı GPU kipi CLI bayrağıyla seçilir (GpuArguments); ortamdan gelen PRODUCER_BROWSER_GPU_MODE=software "Önce GPU"yu ezmesin.
        env["PRODUCER_BROWSER_GPU_MODE"] = "auto";
        env["HYPERFRAMES_SKIP_SKILLS"] = "1";
        env["HYPERFRAMES_CACHE_DIR"] = CacheDir;
        if (env.TryGetValue("FFMPEG_PATH", out var ffmpeg))
        {
            env["HYPERFRAMES_FFMPEG_PATH"] = ffmpeg;
        }

        if (env.TryGetValue("FFPROBE_PATH", out var ffprobe))
        {
            env["HYPERFRAMES_FFPROBE_PATH"] = ffprobe;
        }

        if (Tools.Find(ToolKind.ChromeHeadlessShell) is { } chrome)
        {
            env["HYPERFRAMES_BROWSER_PATH"] = chrome;
        }

        return env;
    }

    /// <inheritdoc />
    protected override async Task<Result<SceneProject>> PrepareProjectAsync(SceneRenderRequest request, CancellationToken ct)
    {
        var dir = Path.Combine(request.WorkDir, string.Create(CultureInfo.InvariantCulture, $"hyperframes-scene-{request.SceneIndex:D3}"));
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        Directory.CreateDirectory(dir);
        var gsap = Path.Combine(TemplateDir, "node_modules", "gsap", "dist", "gsap.min.js");
        if (!File.Exists(gsap))
        {
            return Error.Failure("video.hyperframes.gsap_missing", "GSAP kütüphanesi şablonda bulunamadı; motoru yeniden kurun.");
        }

        File.Copy(gsap, Path.Combine(dir, "gsap.min.js"), overwrite: true);
        CopyBundledFonts(Path.Combine(dir, BundledFontCatalog.FontsFolder));
        var html = ForceRootSize(request.SceneCode, request.Width, request.Height);
        var entry = Path.Combine(dir, "index.html");
        await File.WriteAllTextAsync(entry, html, Utf8NoBom, ct).ConfigureAwait(false);
        return new SceneProject(dir, Path.Combine(dir, "assets"), entry, DeleteAfterRender: false);
    }

    /// <summary>
    /// GPU argümanları ("Önce GPU"): GPU açıkken <c>--gpu</c> (HyperFrames kullanılabilir GPU kodlayıcıyı — NVENC — kendisi yoklar, yoksa
    /// libx264) ve tarayıcı GPU'su HyperFrames varsayılanı olan <c>auto</c> kalır (WebGL yoklaması; donanım varsa ANGLE/D3D11 +
    /// GPU rasterleştirme, yoksa SwiftShader ve "browserGpuMode probe → software" satırı — taban sınıf bunu uyarı olarak loglar).
    /// CPU yedeğinde <c>--no-browser-gpu</c> (SwiftShader) ve yazılım kodlayıcı.
    /// </summary>
    /// <param name="useGpu">GPU açık mı.</param>
    /// <returns>Argümanlar.</returns>
    public static IReadOnlyList<string> GpuArguments(bool useGpu) => useGpu ? ["--gpu"] : ["--no-browser-gpu"];

    /// <inheritdoc />
    protected override Result<IReadOnlyList<string>> BuildRenderArguments(SceneProject project, SceneRenderRequest request, bool useGpu) =>
        new List<string>
        {
            CliPath, "render", project.ProjectDir,
            "-o", Path.GetFullPath(request.OutputPath),
            "--fps", Invariant(request.Fps),
            "--quality", "looks",
            "--format", "mp4",
            "--strict",
        }.Concat(GpuArguments(useGpu)).ToList();

    private static Dictionary<string, string> BuildDependencies()
    {
        var deps = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["hyperframes"] = HyperFramesVersion,
            ["gsap"] = GsapVersion,
        };
        foreach (var font in BundledFontCatalog.Fonts)
        {
            deps[font.PackageName] = BundledFontCatalog.PackageVersion;
        }

        return deps;
    }

    private string? ResolveChrome()
    {
        if (Tools.Find(ToolKind.ChromeHeadlessShell) is { } chrome && File.Exists(chrome))
        {
            return chrome;
        }

        var exe = OperatingSystem.IsWindows() ? "chrome-headless-shell.exe" : "chrome-headless-shell";
        return Directory.Exists(CacheDir) ? FindInTemplate(".hf-cache", exe) : null;
    }

    private static string SetAttribute(string tag, string name, string value)
    {
        var attr = new Regex(@"\s" + Regex.Escape(name) + @"\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (attr.IsMatch(tag))
        {
            return attr.Replace(tag, $" {name}=\"{value}\"", 1);
        }

        var close = tag.EndsWith("/>", StringComparison.Ordinal) ? tag.Length - 2 : tag.Length - 1;
        return string.Concat(tag.AsSpan(0, close), $" {name}=\"{value}\"", tag.AsSpan(close));
    }

    private static string GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    [GeneratedRegex(@"<[a-zA-Z][a-zA-Z0-9-]*\b[^>]*\sdata-composition-id\s*=[^>]*>", RegexOptions.CultureInvariant)]
    private static partial Regex RootTagRegex();
}
