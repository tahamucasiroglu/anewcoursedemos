using System.Text.RegularExpressions;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Domain.Rules;
using Konsept.Agent.Infrastructure.Base;
using Konsept.Agent.Infrastructure.Sandbox;
using Serilog;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// HyperFrames sahne render'ı kısıtlı ortamda (spec 0018 Req 1–2, 4; ADR-0004): iş klasöründe klasör dışını gösteren bağlantı varsa render yok
/// (L2); ortam sıfırdan ve iş klasörüne yönlü (L2); Node izin modeli — dosya okuma yalnız iş klasörü + bileşenler + koruma modülü, yazma
/// yalnız iş klasörü (L6); ağ koruması — ön yüklenen <c>render-guard.mjs</c> ajanın Chromium'una ölü proxy/DNS/WebRTC bayraklarını ekler,
/// Node tarafı ölü <c>HTTP(S)_PROXY</c>, telemetri ve güncelleme kapalı, Chromium sandbox'ı zorunlu (L3 yedek yol, L5); Job Object süre,
/// bellek ve süreç sınırı (L4); ffprobe ile çıktı kapısı (L7). Hiçbir adım yönetici yetkisi istemez.
/// </summary>
/// <param name="installRoot">Ajanın kurulum yeri (<c>%LOCALAPPDATA%\anewconcept</c>).</param>
/// <param name="limits">Render sınırları (ayarlardan).</param>
/// <param name="logger">Teknik log.</param>
public sealed partial class SandboxedSceneRenderer(string installRoot, RenderLimits limits, ILogger logger) : SceneRendererBase(logger)
{
    /// <summary>Koruma modülünün gömülü kaynak adı.</summary>
    public const string GuardResource = "Konsept.Agent.Infrastructure.Sandbox.render-guard.mjs";

    /// <summary>HyperFrames CLI'sinin bileşen klasörüne göre yolu (0.9.43; B-118 ile doğrulandı).</summary>
    public const string HyperFramesCli = "node_modules/hyperframes/bin/hyperframes.mjs";

    /// <summary>Kodun bağlanmadığı ölü proxy (yalnız yerel motor sunucusu çalışır).</summary>
    public const string DeadProxy = "http://127.0.0.1:9";

    private string Components => Path.Combine(installRoot, "components");

    /// <summary>Ajanın Chromium'u.</summary>
    /// <param name="installRoot">Kurulum yeri.</param>
    /// <returns>Yol.</returns>
    public static string BrowserPath(string installRoot) => Path.Combine(installRoot, "components", "chromium", "chrome-headless-shell.exe");

    /// <summary>
    /// Render sürecinin ortamı (L2/L3): ajanın kendi ortamından yalnızca sistem değişkenleri geçer; kullanıcı klasörleri iş klasörüne, proxy
    /// ölü adrese yönlenir; telemetri ve güncelleme denetimi kapalı.
    /// </summary>
    /// <param name="installRoot">Kurulum yeri.</param>
    /// <param name="jobDirectory">İş klasörü.</param>
    /// <returns>Ortam.</returns>
    public static IReadOnlyDictionary<string, string> Environment(string installRoot, string jobDirectory)
    {
        var system = System.Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";
        var home = Path.Combine(jobDirectory, ".home");
        var components = Path.Combine(installRoot, "components");
        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SystemRoot"] = system,
            ["windir"] = system,
            ["PATH"] = Path.Combine(components, "node") + ";" + Path.Combine(system, "System32") + ";" + system,
            ["TEMP"] = Path.Combine(home, "temp"),
            ["TMP"] = Path.Combine(home, "temp"),
            ["HOME"] = home,
            ["USERPROFILE"] = home,
            ["APPDATA"] = Path.Combine(home, "roaming"),
            ["LOCALAPPDATA"] = Path.Combine(home, "local"),
            ["HYPERFRAMES_NO_TELEMETRY"] = "1",
            ["DO_NOT_TRACK"] = "1",
            ["HYPERFRAMES_NO_UPDATE_CHECK"] = "1",
            ["HYPERFRAMES_NO_AUTO_INSTALL"] = "1",
            ["HYPERFRAMES_NO_OPTIONAL_INSTALL"] = "1",
            ["HYPERFRAMES_SKIP_SKILLS"] = "1",
            ["HYPERFRAMES_NO_PREVIEW_DISCOVERY"] = "1",
            ["HYPERFRAMES_CHROME_SANDBOX"] = "1",
            ["HYPERFRAMES_CACHE_DIR"] = Path.Combine(home, "hf-cache"),
            ["PRODUCER_HEADLESS_SHELL_PATH"] = BrowserPath(installRoot),
            ["HYPERFRAMES_FFMPEG_PATH"] = Path.Combine(components, "ffmpeg", "bin", "ffmpeg.exe"),
            ["HYPERFRAMES_FFPROBE_PATH"] = Path.Combine(components, "ffmpeg", "bin", "ffprobe.exe"),
            ["NODE_USE_ENV_PROXY"] = "1",
            ["HTTP_PROXY"] = DeadProxy,
            ["HTTPS_PROXY"] = DeadProxy,
            ["NO_PROXY"] = "localhost,127.0.0.1",
        };
        foreach (var name in new[] { "NUMBER_OF_PROCESSORS", "PROCESSOR_ARCHITECTURE", "OS", "SystemDrive", "PATHEXT" })
        {
            if (System.Environment.GetEnvironmentVariable(name) is { } value)
            {
                env[name] = value;
            }
        }

        return env;
    }

    /// <summary>Node argümanları (L6 izinler + L3 koruma modülü + HyperFrames render).</summary>
    /// <param name="installRoot">Kurulum yeri.</param>
    /// <param name="guard">Koruma modülünün yolu.</param>
    /// <param name="request">İstek.</param>
    /// <returns>Argümanlar.</returns>
    public static IReadOnlyList<string> NodeArguments(string installRoot, string guard, SceneRenderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var components = Path.Combine(installRoot, "components");
        var args = new List<string>
        {
            "--no-warnings",
            "--permission",
            "--allow-fs-read=" + request.JobDirectory,
            "--allow-fs-read=" + components,
            "--allow-fs-read=" + guard,
            "--allow-fs-write=" + request.JobDirectory,
            "--allow-child-process",
            "--allow-worker",
            "--allow-net", // HyperFrames yerel dosya sunucusu (127.0.0.1) için gerekli; dış ağ L3'te kesilir (ölçüldü, plan 0018).
            "--import",
            new Uri(guard).AbsoluteUri,
            Path.Combine(components, "hyperframes", HyperFramesCli),
            "render",
            request.SceneDirectory,
            "--output",
            Path.Combine(request.SceneDirectory, request.ClipName),
            "--fps",
            "30",
            "--resolution",
            request.Height > request.Width ? "portrait" : "landscape",
            "--workers",
            "1",
        };
        if (request.ClipName.EndsWith(".webm", StringComparison.OrdinalIgnoreCase))
        {
            args.AddRange(["--format", "webm"]);
        }

        return args;
    }

    /// <inheritdoc />
    protected override async Task<SceneRenderResult> RenderConfinedAsync(SceneRenderRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var gsap = Directory.EnumerateFiles(Path.Combine(Components, "hyperframes", Path.Combine("node_modules", "hyperframes", "dist", "vendor")), "gsap.min.js", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (gsap is not null)
        {
            File.Copy(gsap, Path.Combine(request.SceneDirectory, "gsap.min.js"), overwrite: true);
        }

        var guard = await WriteGuardAsync(cancellationToken).ConfigureAwait(false);
        var env = Environment(installRoot, request.JobDirectory);
        foreach (var dir in new[] { env["TEMP"], env["APPDATA"], env["LOCALAPPDATA"], env["HYPERFRAMES_CACHE_DIR"] })
        {
            Directory.CreateDirectory(dir);
        }

        var run = await SandboxRunner.RunAsync(
            new SandboxStart(Path.Combine(Components, "node", "node.exe"), NodeArguments(installRoot, guard, request), request.SceneDirectory, env, limits),
            cancellationToken).ConfigureAwait(false);
        if (run.LimitExceeded)
        {
            Logger.Warning("Render sınırı aşıldı; süreç ağacı sonlandırıldı (kalan süreç {Remaining})", run.RemainingProcesses);
            return new SceneRenderResult(SceneRenderOutcome.LimitExceeded);
        }

        var clip = Path.Combine(request.SceneDirectory, request.ClipName);
        if (run.ExitCode != 0 || !File.Exists(clip))
        {
            Logger.Warning("Sahne render edilemedi: çıkış {Exit}", run.ExitCode);
            return new SceneRenderResult(SceneRenderOutcome.Failed, Summary(run.Output, installRoot));
        }

        var probe = await SandboxRunner.RunAsync(
            new SandboxStart(env["HYPERFRAMES_FFPROBE_PATH"], [.. OutputGate.ProbeArguments, clip], request.SceneDirectory, env, limits with { MaxProcesses = 2 }),
            cancellationToken).ConfigureAwait(false);
        var problem = OutputGate.Problem(probe.ExitCode == 0 ? JsonPart(probe.Output) : null, request.Width, request.Height, request.DurationSeconds);
        if (problem is not null)
        {
            Logger.Warning("Render çıktısı çıktı kapısından geçmedi: {Problem}", problem);
            File.Delete(clip);
            return new SceneRenderResult(SceneRenderOutcome.OutputInvalid, problem);
        }

        return new SceneRenderResult(SceneRenderOutcome.Ok);
    }

    /// <summary>Render çıktısından kısa hata özeti (iç yollar gizlenir; düzeltme istemine veri olarak gider).</summary>
    /// <param name="output">Çıktı.</param>
    /// <param name="installRoot">Kurulum yeri (gizlenir).</param>
    /// <returns>Özet.</returns>
    public static string Summary(string output, string installRoot)
    {
        ArgumentNullException.ThrowIfNull(output);
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => !l.StartsWith("@hf-", StringComparison.Ordinal) && !l.StartsWith("[INFO]", StringComparison.Ordinal))
            .ToList();
        var line = lines.LastOrDefault(l => ErrorLine().IsMatch(l)) ?? lines.LastOrDefault() ?? "render error";
        line = line.Replace(installRoot, "<job>", StringComparison.OrdinalIgnoreCase);
        return line.Length > 400 ? line[..400] : line;
    }

    private static string JsonPart(string output)
    {
        var start = output.IndexOf('{', StringComparison.Ordinal);
        var end = output.LastIndexOf('}');
        return start >= 0 && end > start ? output[start..(end + 1)] : string.Empty;
    }

    [GeneratedRegex(@"(?i)error|failed|exception|cannot|unable", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex ErrorLine();

    private async Task<string> WriteGuardAsync(CancellationToken cancellationToken)
    {
        var dir = Path.Combine(installRoot, "sandbox");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "render-guard.mjs");
        await using var source = typeof(SandboxedSceneRenderer).Assembly.GetManifestResourceStream(GuardResource)
            ?? throw new InvalidOperationException("Render koruma modülü ajana gömülü değil.");
        await using var target = File.Create(path);
        await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
        return path;
    }
}
