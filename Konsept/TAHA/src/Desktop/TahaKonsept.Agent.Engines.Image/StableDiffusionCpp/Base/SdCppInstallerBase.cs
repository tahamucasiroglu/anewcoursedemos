using System.Globalization;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Gpu.Abstract;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Tools;
using TahaKonsept.Agent.Abstractions.Tools.Abstract;
using TahaKonsept.Agent.Engines.Image.Downloads;
using TahaKonsept.Agent.Engines.Image.Downloads.Abstract;
using TahaKonsept.Agent.Engines.Image.Options;
using TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Base;

/// <summary>
/// sd.cpp kurulum tabanı: yürütülebilir dosyayı bulma, hızlandırıcı seçimi, sürüm dosyası seçimi, zip indirme (devamlı) ve açma.
/// Alt sınıf yalnızca <see cref="GetLatestReleaseAsync"/> (sürüm bilgisini nereden aldığını) sağlar.
/// Kurulumdan sonra <c>InstallDir/release.txt</c> etiket ve hızlandırıcıyı tutar.
/// </summary>
public abstract class SdCppInstallerBase : ISdCppInstaller
{
    /// <summary>Yeni sürümlerdeki CLI adı.</summary>
    public const string CliExecutableName = "sd-cli.exe";

    /// <summary>Eski sürümlerdeki CLI adı.</summary>
    public const string LegacyExecutableName = "sd.exe";

    private readonly SemaphoreSlim _installLock = new(1, 1);
    private readonly IGpuCapabilityProbe _gpu;
    private readonly IModelFileDownloader _downloader;
    private readonly IToolLocator? _toolLocator;

    /// <summary>Taban oluşturur.</summary>
    /// <param name="gpu">GPU yoklaması.</param>
    /// <param name="downloader">Dosya indirici.</param>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="options">Ayarlar.</param>
    /// <param name="logger">Günlükleyici.</param>
    /// <param name="toolLocator">İsteğe bağlı araç bulucu (PATH'teki sd-cli için).</param>
    protected SdCppInstallerBase(IGpuCapabilityProbe gpu, IModelFileDownloader downloader, IAgentPaths paths, ImageEngineOptions options, ILogger logger, IToolLocator? toolLocator)
    {
        _gpu = gpu;
        _downloader = downloader;
        _toolLocator = toolLocator;
        Options = options;
        Logger = logger;
        InstallDir = Path.Combine(paths.ToolsDir, options.SdCppToolSubdirectory);
    }

    /// <inheritdoc />
    public string InstallDir { get; }

    /// <summary>Ayarlar.</summary>
    protected ImageEngineOptions Options { get; }

    /// <summary>Günlükleyici.</summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public string? FindExecutable()
    {
        foreach (var name in new[] { CliExecutableName, LegacyExecutableName })
        {
            var candidate = Path.Combine(InstallDir, name);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var located = _toolLocator?.Find(ToolKind.StableDiffusionCpp);
        return located is not null && File.Exists(located) && !located.EndsWith("sd-server.exe", StringComparison.OrdinalIgnoreCase)
            ? located
            : null;
    }

    /// <inheritdoc />
    public SdCppBackend ResolveBackend() => Options.SdCppBackend switch
    {
        SdCppBackendPreference.Cuda => SdCppBackend.Cuda,
        SdCppBackendPreference.Vulkan => SdCppBackend.Vulkan,
        SdCppBackendPreference.Cpu => SdCppBackend.Cpu,
        _ when _gpu.HasNvidiaDriver => SdCppBackend.Cuda,
        _ when _gpu.HasVulkanRuntime => SdCppBackend.Vulkan,
        _ => SdCppBackend.Cpu,
    };

    /// <inheritdoc />
    public virtual IReadOnlyList<GitHubReleaseAsset> SelectAssets(GitHubRelease release, SdCppBackend backend)
    {
        var windows = release.Assets
            .Where(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && a.Name.Contains("win", StringComparison.OrdinalIgnoreCase))
            .ToList();

        GitHubReleaseAsset? Pick(params string[] markers) =>
            markers.Select(m => windows.FirstOrDefault(a => a.Name.Contains(m, StringComparison.OrdinalIgnoreCase)
                    && !a.Name.StartsWith("cudart", StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault(a => a is not null);

        switch (backend)
        {
            case SdCppBackend.Cuda:
                var main = Pick("-bin-win-cuda12-x64", "-bin-win-cuda");
                if (main is null)
                {
                    return [];
                }

                var major = main.Name.Contains("cuda12", StringComparison.OrdinalIgnoreCase) ? "cu12" : "cu";
                var runtime = windows.FirstOrDefault(a => a.Name.StartsWith("cudart", StringComparison.OrdinalIgnoreCase)
                    && a.Name.Contains(major, StringComparison.OrdinalIgnoreCase));
                return runtime is null ? [main] : [main, runtime];
            case SdCppBackend.Vulkan:
                return Pick("-bin-win-vulkan-x64") is { } vulkan ? [vulkan] : [];
            default:
                return Pick("-bin-win-cpu-x64", "-bin-win-avx2-x64") is { } cpu ? [cpu] : [];
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> EnsureInstalledAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        if (FindExecutable() is { } existing)
        {
            return existing;
        }

        await _installLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (FindExecutable() is { } raced)
            {
                return raced;
            }

            return await InstallAsync(progress, ct).ConfigureAwait(false);
        }
        finally
        {
            _installLock.Release();
        }
    }

    /// <summary>Son sürüm bilgisini getirir.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sürüm ya da hata.</returns>
    protected abstract Task<Result<GitHubRelease>> GetLatestReleaseAsync(CancellationToken ct);

    private async Task<Result<string>> InstallAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new EngineSetupProgress("stable-diffusion.cpp sürüm bilgisi alınıyor…", null));
        var release = await GetLatestReleaseAsync(ct).ConfigureAwait(false);
        if (release.IsFailure)
        {
            return release.Error;
        }

        var backend = ResolveBackend();
        var assets = SelectAssets(release.Value, backend);
        if (assets.Count == 0 && backend != SdCppBackend.Cpu)
        {
            Logger.LogWarning("sd.cpp {Backend} derlemesi bulunamadı, Vulkan/CPU deneniyor", backend);
            backend = backend == SdCppBackend.Cuda && _gpu.HasVulkanRuntime ? SdCppBackend.Vulkan : SdCppBackend.Cpu;
            assets = SelectAssets(release.Value, backend);
        }

        if (assets.Count == 0)
        {
            return Error.NotFound("sdcpp.asset.missing", $"stable-diffusion.cpp {release.Value.TagName} sürümünde Windows ikilisi bulunamadı.");
        }

        Directory.CreateDirectory(InstallDir);
        var downloadDir = Path.Combine(InstallDir, "_download");
        Directory.CreateDirectory(downloadDir);
        var total = assets.Sum(a => a.Size);
        long done = 0;

        foreach (var asset in assets)
        {
            var zipPath = Path.Combine(downloadDir, Path.GetFileName(asset.Name));
            var baseDone = done;
            var fileProgress = progress is null
                ? null
                : new Progress<ModelDownloadProgress>(p => progress.Report(new EngineSetupProgress(
                    string.Create(CultureInfo.InvariantCulture, $"stable-diffusion.cpp ({backend}) indiriliyor: {asset.Name} ({(baseDone + p.BytesReceived) / 1048576} / {total / 1048576} MB)"),
                    total > 0 ? Math.Min(99d, (baseDone + p.BytesReceived) * 100d / total) : null)));

            Logger.LogInformation("sd.cpp indiriliyor: {Asset} ({Bytes} bayt)", asset.Name, asset.Size);
            var download = await _downloader.DownloadAsync(new Uri(asset.BrowserDownloadUrl), zipPath, asset.Size, null, fileProgress, ct).ConfigureAwait(false);
            if (download.IsFailure)
            {
                return download.Error;
            }

            progress?.Report(new EngineSetupProgress($"{asset.Name} açılıyor…", null));
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, InstallDir, overwriteFiles: true), ct).ConfigureAwait(false);
            File.Delete(zipPath);
            done += asset.Size;
        }

        await File.WriteAllTextAsync(Path.Combine(InstallDir, "release.txt"), $"{release.Value.TagName}\n{backend}\n", ct).ConfigureAwait(false);
        TryDeleteDirectory(downloadDir);

        var executable = FindExecutable();
        if (executable is null)
        {
            return Error.Failure("sdcpp.install.noexe", $"stable-diffusion.cpp açıldı ama {CliExecutableName} bulunamadı ({InstallDir}).");
        }

        Logger.LogInformation("sd.cpp kuruldu: {Tag} {Backend} → {Path}", release.Value.TagName, backend, executable);
        progress?.Report(new EngineSetupProgress($"stable-diffusion.cpp ({backend}) kuruldu.", 100));
        return executable;
    }

    private void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
            {
                Directory.Delete(path);
            }
        }
        catch (IOException ex)
        {
            Logger.LogDebug(ex, "Geçici klasör silinemedi: {Path}", path);
        }
    }
}
