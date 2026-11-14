using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Abstractions.Video;
using TahaKonsept.Agent.Abstractions.Video.Abstract;
using TahaKonsept.Agent.Engines.Video.Fonts;
using TahaKonsept.Agent.Engines.Video.Media.Abstract;
using TahaKonsept.Agent.Engines.Video.Safety.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Video.Engines.Base;

/// <summary>
/// Node tabanlı video motorlarının (HyperFrames, Remotion, Revideo) ortak tabanı.
/// <list type="bullet">
/// <item><see cref="EnsureReadyAsync"/>: <c>ToolsDir/engines/&lt;key&gt;</c> altında sürümleri sabit bir şablon projesi
/// (<c>package.json</c> + <c>npm install</c>) kurar, ardından <see cref="InstallRuntimeAsync"/> ile tarayıcı vb. ek bileşenleri indirir.</item>
/// <item><see cref="RenderSceneAsync"/>: kodu güvenlik denetiminden geçirir, <see cref="PrepareProjectAsync"/> ile sahne projesini
/// yazar, görselleri kopyalar, CLI'ı node ile çalıştırır, ilerleme satırlarını <see cref="ParseProgress"/> ile yüzdeye çevirir
/// ve çıktının gerçek süresini ffprobe ile ölçer.</item>
/// <item><see cref="ValidateSceneAsync"/>: statik güvenlik denetimi + yapı kontrolü + motorun kendi doğrulaması (<see cref="ValidateWithToolAsync"/>).</item>
/// </list>
/// Telemetri ortam değişkenleriyle kapatılır; ağdan bağımsız çalışma için fontlar ve kütüphaneler şablondan kopyalanır.
/// "Önce GPU": render önce GPU açık argümanlarla yapılır (<see cref="BuildRenderArguments"/> <c>useGpu=true</c>: Chrome ANGLE/D3D11 GPU
/// rasterleştirme, motor destekliyorsa NVENC). Başarısız olur ve günlük GPU kaynaklı görünürse (<see cref="IsLikelyGpuFailure"/>)
/// uyarı loglanır ve sahne CPU (SwiftShader/libx264) ile yeniden render edilir; bu süreçte motor artık CPU ile başlar. Başarılı
/// render'da aracın kendi CPU'ya düşüşü (<see cref="DetectGpuFallback"/>, ör. HyperFrames WebGL yoklaması) de uyarı olarak loglanır.
/// </summary>
public abstract class NodeVideoEngineBase : IVideoEngine
{
    private const string StampFileName = ".tk-template.json";

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> InstallLocks = new(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] GpuFailureMarkers =
    [
        "gpu", "angle", "d3d11", "webgl", "nvenc", "swiftshader", "context lost", "failed to launch", "browser process",
        "target closed", "crashed", "cuda",
    ];

    private static readonly string[] GpuFallbackMarkers =
    [
        "browserGpuMode probe → software", "Falling back to software",
        "GPU process isn't usable", "falling back to SwiftShader",
    ];

    private volatile string? _gpuDisabledReason;

    /// <summary>Taban oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="tools">Araç bulucu (node, npm, ffmpeg, chrome).</param>
    /// <param name="runner">Süreç çalıştırıcı.</param>
    /// <param name="probe">Medya süresi okuyucu.</param>
    /// <param name="guard">Sahne kodu güvenlik denetçisi.</param>
    /// <param name="logger">Günlükçü.</param>
    protected NodeVideoEngineBase(IAgentPaths paths, IToolLocator tools, IProcessRunner runner, IMediaProbe probe, ISceneCodeGuard guard, ILogger logger)
    {
        Paths = paths;
        Tools = tools;
        Runner = runner;
        Probe = probe;
        Guard = guard;
        Logger = logger;
    }

    /// <inheritdoc />
    public abstract string Key { get; }

    /// <inheritdoc />
    public abstract string DisplayName { get; }

    /// <inheritdoc />
    public virtual int? MinVramMb => null;

    /// <summary>Şablon proje klasörü: <c>ToolsDir/engines/&lt;Key&gt;</c>.</summary>
    public string TemplateDir => Path.Combine(Paths.ToolsDir, "engines", Key);

    /// <summary>BOM'suz UTF-8.</summary>
    protected static Encoding Utf8NoBom { get; } = new UTF8Encoding(false);

    /// <summary>Ajan klasörleri.</summary>
    protected IAgentPaths Paths { get; }

    /// <summary>Araç bulucu.</summary>
    protected IToolLocator Tools { get; }

    /// <summary>Süreç çalıştırıcı.</summary>
    protected IProcessRunner Runner { get; }

    /// <summary>Medya süresi okuyucu.</summary>
    protected IMediaProbe Probe { get; }

    /// <summary>Güvenlik denetçisi.</summary>
    protected ISceneCodeGuard Guard { get; }

    /// <summary>Günlükçü.</summary>
    protected ILogger Logger { get; }

    /// <summary>Sabit sürümlü npm bağımlılıkları (paket → tam sürüm).</summary>
    protected abstract IReadOnlyDictionary<string, string> Dependencies { get; }

    /// <summary>Şablonun CLI giriş dosyası (şablon klasörüne göre), ör. <c>node_modules/hyperframes/bin/hyperframes.mjs</c>.</summary>
    protected abstract string CliRelativePath { get; }

    /// <summary>Şablon <c>package.json</c> dosyasında <c>"type": "module"</c> olsun mu.</summary>
    protected virtual bool UsesEsModules => false;

    /// <summary>Tek sahne render zaman aşımı.</summary>
    protected virtual TimeSpan RenderTimeout => TimeSpan.FromMinutes(30);

    /// <summary>npm install zaman aşımı.</summary>
    protected virtual TimeSpan InstallTimeout => TimeSpan.FromMinutes(20);

    /// <summary>CLI tam yolu.</summary>
    protected string CliPath => Path.Combine(TemplateDir, CliRelativePath.Replace('/', Path.DirectorySeparatorChar));

    /// <inheritdoc />
    public bool IsInstalled() => PackagesCurrent() && IsRuntimeReady();

    /// <inheritdoc />
    public async Task<Result> EnsureReadyAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        var gate = InstallLocks.GetOrAdd(TemplateDir, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (IsInstalled())
            {
                return Result.Success();
            }

            var node = Tools.Require(ToolKind.Node);
            if (node.IsFailure)
            {
                return node.Error;
            }

            Directory.CreateDirectory(TemplateDir);
            var packageJson = BuildPackageJson();
            if (!PackagesCurrent())
            {
                progress?.Report(new EngineSetupProgress($"{DisplayName}: npm paketleri kuruluyor…", 5));
                await File.WriteAllTextAsync(Path.Combine(TemplateDir, "package.json"), packageJson, Utf8NoBom, ct).ConfigureAwait(false);
                await File.WriteAllTextAsync(Path.Combine(TemplateDir, ".npmrc"), "fund=false\naudit=false\nupdate-notifier=false\nsave-exact=true\n", Utf8NoBom, ct).ConfigureAwait(false);

                var npm = BuildNpmSpec(["install", "--no-audit", "--no-fund", "--loglevel=error"], TemplateDir, InstallTimeout);
                if (npm.IsFailure)
                {
                    return npm.Error;
                }

                var install = await Runner.RunAsync(npm.Value, line => Logger.LogDebug("[{Engine} npm] {Line}", Key, line), ct).ConfigureAwait(false);
                if (!install.Succeeded || !File.Exists(CliPath))
                {
                    return Error.Failure($"video.{Key}.npm_install_failed", $"{DisplayName} paketleri kurulamadı (kod {install.ExitCode}): {Tail(install.StandardError, install.StandardOutput)}");
                }

                await File.WriteAllTextAsync(Path.Combine(TemplateDir, StampFileName), packageJson, Utf8NoBom, ct).ConfigureAwait(false);
            }

            progress?.Report(new EngineSetupProgress($"{DisplayName}: çalışma bileşenleri hazırlanıyor…", 70));
            var runtime = await InstallRuntimeAsync(progress, ct).ConfigureAwait(false);
            if (runtime.IsFailure)
            {
                return runtime;
            }

            progress?.Report(new EngineSetupProgress($"{DisplayName} hazır.", 100));
            Logger.LogInformation("{Engine} motoru hazır: {Dir}", Key, TemplateDir);
            return Result.Success();
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Result<VideoRenderOutput>> RenderSceneAsync(SceneRenderRequest request, IProgress<double>? progress, CancellationToken ct)
    {
        var valid = ValidateRequest(request);
        if (valid.IsFailure)
        {
            return Result.Failure<VideoRenderOutput>(valid.Error);
        }

        var guard = Guard.Check(request.SceneCode);
        if (guard.IsFailure)
        {
            return Result.Failure<VideoRenderOutput>(guard.Error);
        }

        var ready = await EnsureReadyAsync(null, ct).ConfigureAwait(false);
        if (ready.IsFailure)
        {
            return Result.Failure<VideoRenderOutput>(ready.Error);
        }

        var node = Tools.Require(ToolKind.Node);
        if (node.IsFailure)
        {
            return Result.Failure<VideoRenderOutput>(node.Error);
        }

        Directory.CreateDirectory(request.WorkDir);
        var project = await PrepareProjectAsync(request, ct).ConfigureAwait(false);
        if (project.IsFailure)
        {
            return Result.Failure<VideoRenderOutput>(project.Error);
        }

        try
        {
            return await RenderPreparedAsync(node.Value, project.Value, request, progress, ct).ConfigureAwait(false);
        }
        finally
        {
            if (project.Value.DeleteAfterRender)
            {
                TryDeleteDirectory(project.Value.ProjectDir);
            }
        }
    }

    /// <inheritdoc />
    public async Task<Result> ValidateSceneAsync(SceneRenderRequest request, CancellationToken ct)
    {
        var guard = Guard.Check(request.SceneCode);
        if (guard.IsFailure)
        {
            return guard;
        }

        var structure = ValidateStructure(request.SceneCode);
        if (structure.IsFailure)
        {
            return structure;
        }

        return IsInstalled() ? await ValidateWithToolAsync(request, ct).ConfigureAwait(false) : Result.Success();
    }

    /// <summary>Şablon <c>package.json</c> içeriğini (sıralı, deterministik) üretir.</summary>
    /// <returns>JSON metni.</returns>
    public string BuildPackageJson()
    {
        var deps = Dependencies.OrderBy(d => d.Key, StringComparer.Ordinal).ToDictionary(d => d.Key, d => d.Value);
        var doc = new Dictionary<string, object>
        {
            ["name"] = "tk-engine-" + Key,
            ["private"] = true,
            ["version"] = "1.0.0",
        };
        if (UsesEsModules)
        {
            doc["type"] = "module";
        }

        doc["dependencies"] = deps;
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>Bir çıktı satırından 0–100 ilerleme çıkarır; ilerleme yoksa <c>null</c>.</summary>
    /// <param name="line">Satır.</param>
    /// <returns>Yüzde ya da <c>null</c>.</returns>
    public abstract double? ParseProgress(string line);

    /// <summary>İstek alanlarını denetler (boyut, fps, süre, çıktı yolu).</summary>
    /// <param name="request">İstek.</param>
    /// <returns>Başarı ya da doğrulama hatası.</returns>
    protected static Result ValidateRequest(SceneRenderRequest request)
    {
        if (request.Width < 16 || request.Height < 16 || request.Width % 2 != 0 || request.Height % 2 != 0)
        {
            return Error.Validation("video.invalid_size", "Genişlik ve yükseklik çift sayı ve en az 16 olmalı.");
        }

        if (request.Fps is < 1 or > 120)
        {
            return Error.Validation("video.invalid_fps", "Kare hızı 1–120 arasında olmalı.");
        }

        if (request.DurationSec is <= 0 or > 600)
        {
            return Error.Validation("video.invalid_duration", "Sahne süresi 0–600 sn arasında olmalı.");
        }

        return string.IsNullOrWhiteSpace(request.OutputPath) || string.IsNullOrWhiteSpace(request.WorkDir)
            ? Error.Validation("video.invalid_paths", "Çalışma klasörü ve çıktı yolu zorunlu.")
            : Result.Success();
    }

    /// <summary>Hata metinleri için çıktının son kısmı.</summary>
    /// <param name="primary">Öncelikli metin (stderr).</param>
    /// <param name="fallback">Yedek metin (stdout).</param>
    /// <returns>En fazla ~1500 karakter.</returns>
    protected static string Tail(string primary, string fallback)
    {
        var text = (string.IsNullOrWhiteSpace(primary) ? fallback : primary).Trim();
        return text.Length <= 1500 ? text : text[^1500..];
    }

    /// <summary>Kültürden bağımsız sayı biçimi.</summary>
    /// <param name="value">Değer.</param>
    /// <returns>Metin.</returns>
    protected static string Invariant(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Kültürden bağımsız tam sayı biçimi.</summary>
    /// <param name="value">Değer.</param>
    /// <returns>Metin.</returns>
    protected static string Invariant(int value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Motor dışı yapısal kontrol (ör. default export var mı). Varsayılan: başarı.</summary>
    /// <param name="sceneCode">Sahne kodu.</param>
    /// <returns>Başarı ya da doğrulama hatası.</returns>
    protected virtual Result ValidateStructure(string sceneCode) => Result.Success();

    /// <summary>Motorun kendi doğrulama aracı (lint). Varsayılan: başarı.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Başarı ya da ayrıntılı hata.</returns>
    protected virtual Task<Result> ValidateWithToolAsync(SceneRenderRequest request, CancellationToken ct) => Task.FromResult(Result.Success());

    /// <summary>Paketler dışında gereken çalışma bileşenleri (headless Chrome) hazır mı.</summary>
    /// <returns>Hazırsa <c>true</c>.</returns>
    protected virtual bool IsRuntimeReady() => true;

    /// <summary>Paketlerden sonra ek bileşenleri kurar (ör. <c>browser ensure</c>). Varsayılan: hiçbir şey.</summary>
    /// <param name="progress">İlerleme.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Başarı ya da hata.</returns>
    protected virtual Task<Result> InstallRuntimeAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct) => Task.FromResult(Result.Success());

    /// <summary>Sahne projesini (kod, kütüphaneler, fontlar) diske yazar.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Hazır proje.</returns>
    protected abstract Task<Result<SceneProject>> PrepareProjectAsync(SceneRenderRequest request, CancellationToken ct);

    /// <summary>node'a verilecek argümanlar (ilk argüman çalıştırılacak betik).</summary>
    /// <param name="project">Proje.</param>
    /// <param name="request">İstek.</param>
    /// <param name="useGpu">GPU hızlandırma açık mı ("Önce GPU"; <c>false</c> = CPU yedeği: SwiftShader / yazılım kodlayıcı).</param>
    /// <returns>Argümanlar.</returns>
    protected abstract Result<IReadOnlyList<string>> BuildRenderArguments(SceneProject project, SceneRenderRequest request, bool useGpu);

    /// <summary>Motor GPU hızlandırmayı denesin mi (varsayılan <c>true</c>).</summary>
    protected virtual bool PreferGpu => true;

    /// <summary>Başarısız bir render günlüğü GPU/tarayıcı kaynaklı mı görünüyor (CPU ile yeniden denemeye değer mi).</summary>
    /// <param name="log">Render günlüğü (stdout + stderr).</param>
    /// <returns>GPU kaynaklı görünüyorsa <c>true</c>.</returns>
    public static bool IsLikelyGpuFailure(string? log) =>
        !string.IsNullOrEmpty(log) && GpuFailureMarkers.Any(m => log.Contains(m, StringComparison.OrdinalIgnoreCase));

    /// <summary>Başarılı render günlüğünde aracın kendi GPU → CPU düşüşünü arar (ör. HyperFrames "browserGpuMode probe → software").</summary>
    /// <param name="log">Render günlüğü.</param>
    /// <returns>Düşüşü anlatan satır ya da <c>null</c>.</returns>
    public static string? DetectGpuFallback(string? log)
    {
        if (string.IsNullOrEmpty(log))
        {
            return null;
        }

        foreach (var line in log.Split('\n'))
        {
            if (GpuFallbackMarkers.Any(m => line.Contains(m, StringComparison.OrdinalIgnoreCase)))
            {
                return line.Trim();
            }
        }

        return null;
    }

    /// <summary>Render bittikten sonra çıktıyı hedef yola taşır (gerekiyorsa). Varsayılan: hiçbir şey.</summary>
    /// <param name="project">Proje.</param>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Başarı ya da hata.</returns>
    protected virtual Task<Result> FinalizeOutputAsync(SceneProject project, SceneRenderRequest request, CancellationToken ct) => Task.FromResult(Result.Success());

    /// <summary>Alt süreçlere verilen ortam değişkenleri: PATH (node + ffmpeg klasörü), telemetri kapatma, ffmpeg yolları.</summary>
    /// <returns>Ortam değişkenleri.</returns>
    protected virtual Dictionary<string, string> BuildEnvironment()
    {
        var env = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DO_NOT_TRACK"] = "1",
            ["NO_UPDATE_NOTIFIER"] = "1",
            ["npm_config_update_notifier"] = "false",
            ["npm_config_fund"] = "false",
            ["npm_config_audit"] = "false",
            ["PUPPETEER_SKIP_DOWNLOAD"] = "true",
            ["DISABLE_TELEMETRY"] = "true",
        };

        var pathParts = new List<string>();
        if (Tools.Find(ToolKind.Node) is { } node && Path.GetDirectoryName(node) is { Length: > 0 } nodeDir)
        {
            pathParts.Add(nodeDir);
        }

        if (Tools.Find(ToolKind.Ffmpeg) is { } ffmpeg)
        {
            env["FFMPEG_PATH"] = ffmpeg;
            if (Path.GetDirectoryName(ffmpeg) is { Length: > 0 } ffmpegDir)
            {
                pathParts.Add(ffmpegDir);
            }
        }

        if (Tools.Find(ToolKind.Ffprobe) is { } ffprobe)
        {
            env["FFPROBE_PATH"] = ffprobe;
        }

        pathParts.Add(Environment.GetEnvironmentVariable("PATH") ?? string.Empty);
        env["PATH"] = string.Join(Path.PathSeparator, pathParts.Where(p => p.Length > 0));
        return env;
    }

    /// <summary>npm komutu için süreç tanımı üretir (npm-cli.js node ile çalıştırılır).</summary>
    /// <param name="args">npm argümanları.</param>
    /// <param name="workingDirectory">Çalışma klasörü.</param>
    /// <param name="timeout">Zaman aşımı.</param>
    /// <returns>Süreç tanımı ya da hata.</returns>
    protected Result<ProcessSpec> BuildNpmSpec(IReadOnlyList<string> args, string workingDirectory, TimeSpan timeout)
    {
        var node = Tools.Require(ToolKind.Node);
        if (node.IsFailure)
        {
            return Result.Failure<ProcessSpec>(node.Error);
        }

        var npmCli = ResolveNpmCli(node.Value, Tools.Find(ToolKind.Npm));
        if (npmCli is null)
        {
            return Error.Failure("video.npm_missing", "npm bulunamadı; Node.js bileşenini yeniden kurun.");
        }

        var env = BuildEnvironment();
        return npmCli.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
            ? new ProcessSpec(node.Value, [npmCli, .. args], workingDirectory, env, timeout)
            : new ProcessSpec(npmCli, args, workingDirectory, env, timeout);
    }

    /// <summary>Bir node betiği çalıştırır (şablon klasöründe, motor ortamıyla).</summary>
    /// <param name="args">node argümanları (ilk argüman betik).</param>
    /// <param name="timeout">Zaman aşımı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Süreç sonucu ya da hata.</returns>
    protected async Task<Result<ProcessResult>> RunNodeAsync(IReadOnlyList<string> args, TimeSpan timeout, CancellationToken ct)
    {
        var node = Tools.Require(ToolKind.Node);
        if (node.IsFailure)
        {
            return Result.Failure<ProcessResult>(node.Error);
        }

        var spec = new ProcessSpec(node.Value, args, TemplateDir, BuildEnvironment(), timeout);
        return await Runner.RunAsync(spec, line => Logger.LogDebug("[{Engine}] {Line}", Key, line), ct).ConfigureAwait(false);
    }

    /// <summary>Paketlenen yazı tiplerini hedef klasöre kopyalar ve <c>tk-fonts.css</c> yazar.</summary>
    /// <param name="targetFontsDir">Hedef klasör.</param>
    /// <param name="urlPrefix">CSS içindeki url öneki.</param>
    protected void CopyBundledFonts(string targetFontsDir, string urlPrefix = "") =>
        BundledFontCatalog.CopyInto(TemplateDir, targetFontsDir, urlPrefix);

    /// <summary>Şablon içinde benzersiz geçici proje klasörü oluşturur (<c>work/&lt;guid&gt;</c>); node_modules buradan çözülür.</summary>
    /// <returns>Klasör yolu.</returns>
    protected string CreateTemplateWorkDir()
    {
        var dir = Path.Combine(TemplateDir, "work", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Şablon klasörü altında (alt klasörler dahil) verilen adlı ilk dosyayı arar.</summary>
    /// <param name="relativeDir">Şablona göre aranacak klasör.</param>
    /// <param name="fileName">Dosya adı.</param>
    /// <returns>Tam yol ya da <c>null</c>.</returns>
    protected string? FindInTemplate(string relativeDir, string fileName)
    {
        var dir = Path.Combine(TemplateDir, relativeDir);
        return Directory.Exists(dir)
            ? Directory.EnumerateFiles(dir, fileName, SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }

    private bool PackagesCurrent()
    {
        var stamp = Path.Combine(TemplateDir, StampFileName);
        return File.Exists(stamp)
            && string.Equals(File.ReadAllText(stamp), BuildPackageJson(), StringComparison.Ordinal)
            && File.Exists(CliPath);
    }

    private async Task<Result<VideoRenderOutput>> RenderPreparedAsync(string node, SceneProject project, SceneRenderRequest request, IProgress<double>? progress, CancellationToken ct)
    {
        CopyAssets(request.AssetPaths, project.AssetsDir);
        var outputDir = Path.GetDirectoryName(Path.GetFullPath(request.OutputPath));
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        if (File.Exists(request.OutputPath))
        {
            File.Delete(request.OutputPath);
        }

        var useGpu = PreferGpu && _gpuDisabledReason is null;
        if (PreferGpu && !useGpu)
        {
            Logger.LogWarning("{Engine}: GPU hızlandırma bu oturumda kapalı, CPU ile render ediliyor — {Reason}", Key, _gpuDisabledReason);
        }

        var attempt = await RunRenderOnceAsync(node, project, request, useGpu, progress, ct).ConfigureAwait(false);
        if (attempt.IsFailure)
        {
            return Result.Failure<VideoRenderOutput>(attempt.Error);
        }

        var (result, logText) = attempt.Value;
        if (!result.Succeeded && useGpu && !result.TimedOut && !ct.IsCancellationRequested && IsLikelyGpuFailure(logText))
        {
            var why = Tail(result.StandardError, result.StandardOutput);
            _gpuDisabledReason = why.Length <= 300 ? why : why[^300..];
            Logger.LogWarning("{Engine}: GPU hızlandırmalı render başarısız (kod {Code}), sahne CPU ile yeniden render ediliyor — {Reason}", Key, result.ExitCode, _gpuDisabledReason);
            if (File.Exists(request.OutputPath))
            {
                File.Delete(request.OutputPath);
            }

            attempt = await RunRenderOnceAsync(node, project, request, useGpu: false, progress, ct).ConfigureAwait(false);
            if (attempt.IsFailure)
            {
                return Result.Failure<VideoRenderOutput>(attempt.Error);
            }

            (result, logText) = attempt.Value;
        }
        else if (result.Succeeded && useGpu && DetectGpuFallback(logText) is { } toolFallback)
        {
            Logger.LogWarning("{Engine}: araç GPU yerine CPU'ya düştü — {Line}", Key, toolFallback);
        }

        var finalize = result.Succeeded ? await FinalizeOutputAsync(project, request, ct).ConfigureAwait(false) : Result.Success();
        if (!result.Succeeded || finalize.IsFailure || !File.Exists(request.OutputPath))
        {
            var reason = result.TimedOut ? "zaman aşımı" : $"kod {result.ExitCode}";
            var detail = finalize.IsFailure ? finalize.Error.Message : Tail(result.StandardError, result.StandardOutput);
            return Error.Failure($"video.{Key}.render_failed", $"{DisplayName} sahne {request.SceneIndex + 1} render edilemedi ({reason}): {detail}");
        }

        var duration = await Probe.GetDurationAsync(request.OutputPath, ct).ConfigureAwait(false);
        progress?.Report(100);
        return new VideoRenderOutput(request.OutputPath, duration.IsSuccess ? duration.Value : request.DurationSec);
    }

    private async Task<Result<(ProcessResult Result, string Log)>> RunRenderOnceAsync(
        string node, SceneProject project, SceneRenderRequest request, bool useGpu, IProgress<double>? progress, CancellationToken ct)
    {
        var args = BuildRenderArguments(project, request, useGpu);
        if (args.IsFailure)
        {
            return Result.Failure<(ProcessResult, string)>(args.Error);
        }

        var spec = new ProcessSpec(node, args.Value, project.ProjectDir, BuildEnvironment(), RenderTimeout);
        var last = -1d;
        var log = new StringBuilder();
        var result = await Runner.RunAsync(spec, line =>
        {
            // stdout ve stderr geri çağrıları farklı iş parçacıklarından aynı anda gelir: günlük ve ilerleme kilit altında güncellenir
            // (aksi hâlde StringBuilder bozulur/satırlar kaybolur, ilerleme geri gidebilir).
            lock (log)
            {
                if (log.Length < 2_000_000)
                {
                    log.AppendLine(line);
                }

                if (ParseProgress(line) is { } p && p > last)
                {
                    last = Math.Clamp(p, 0, 99);
                    progress?.Report(last);
                }
            }
        }, ct).ConfigureAwait(false);

        string logText;
        lock (log)
        {
            logText = log.ToString();
        }

        var suffix = useGpu ? string.Empty : "-cpu";
        var logPath = Path.Combine(request.WorkDir, string.Create(CultureInfo.InvariantCulture, $"{Key}-scene-{request.SceneIndex:D3}{suffix}.log"));
        await File.WriteAllTextAsync(logPath, logText, Utf8NoBom, CancellationToken.None).ConfigureAwait(false);
        return (result, logText);
    }

    private static string? ResolveNpmCli(string nodePath, string? npmFromLocator)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrEmpty(npmFromLocator))
        {
            if (npmFromLocator.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(npmFromLocator);
            }
            else if (Path.GetDirectoryName(npmFromLocator) is { Length: > 0 } npmDir)
            {
                candidates.Add(Path.Combine(npmDir, "node_modules", "npm", "bin", "npm-cli.js"));
            }
        }

        if (Path.GetDirectoryName(nodePath) is { Length: > 0 } nodeDir)
        {
            candidates.Add(Path.Combine(nodeDir, "node_modules", "npm", "bin", "npm-cli.js"));
            candidates.Add(Path.GetFullPath(Path.Combine(nodeDir, "..", "lib", "node_modules", "npm", "bin", "npm-cli.js")));
        }

        var found = candidates.FirstOrDefault(File.Exists);
        if (found is not null)
        {
            return found;
        }

        return !string.IsNullOrEmpty(npmFromLocator) && !OperatingSystem.IsWindows() ? npmFromLocator : null;
    }

    /// <summary>Sahne görsellerini proje (ya da doğrulama) klasörünün <c>assets/</c> alt klasörüne kopyalar (dosya adı korunur).</summary>
    /// <param name="assets">Görsel dosyaları.</param>
    /// <param name="assetsDir">Hedef klasör.</param>
    protected static void CopyAssets(IReadOnlyList<string> assets, string assetsDir)
    {
        ArgumentNullException.ThrowIfNull(assets);
        Directory.CreateDirectory(assetsDir);
        foreach (var asset in assets.Where(File.Exists))
        {
            File.Copy(asset, Path.Combine(assetsDir, Path.GetFileName(asset)), overwrite: true);
        }
    }

    private void TryDeleteDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (IOException ex)
        {
            Logger.LogWarning(ex, "Geçici proje klasörü silinemedi: {Dir}", dir);
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Geçici proje klasörü silinemedi: {Dir}", dir);
        }
    }
}
