using System.IO.Compression;
using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Downloads.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Prerequisites.Base;

/// <summary>
/// İndirilen (portable zip) ön gereksinim tabanı. Akış: <see cref="ResolveDownloadAsync"/> → <c>Tools/_downloads</c>'a indir
/// (%0–80, hash biliniyorsa SHA256 doğrulama) → <c>Tools/_staging</c>'e aç → tek kök klasör varsa düzleştir →
/// <c>Tools/&lt;TargetDirectoryName&gt;</c>'e taşı → <c>.tk-version</c> yaz → <see cref="ExecutableRelativePath"/> ile doğrula.
/// Kurulu olma denetimi yalnızca ajanın kendi klasörüne bakar (PATH'teki farklı sürüm sayılmaz).
/// </summary>
public abstract class DownloadablePrerequisiteBase : PrerequisiteBase
{
    /// <summary>Sürüm işaret dosyası.</summary>
    public const string VersionMarkerFile = ".tk-version";

    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="downloader">İndirici.</param>
    protected DownloadablePrerequisiteBase(IAgentPaths paths, IFileDownloader downloader)
    {
        Paths = paths ?? throw new ArgumentNullException(nameof(paths));
        Downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
    }

    /// <summary>Ajan klasörleri.</summary>
    protected IAgentPaths Paths { get; }

    /// <summary>İndirici.</summary>
    protected IFileDownloader Downloader { get; }

    /// <summary><c>Tools</c> altındaki hedef klasör adı (ör. <c>node</c>).</summary>
    protected abstract string TargetDirectoryName { get; }

    /// <summary>Hedef klasöre göre doğrulama dosyası (ör. <c>node.exe</c>, <c>bin/ffmpeg.exe</c>).</summary>
    protected abstract string ExecutableRelativePath { get; }

    /// <summary>Hedef klasörün tam yolu.</summary>
    protected string TargetDirectory => Path.Combine(Paths.ToolsDir, TargetDirectoryName);

    /// <inheritdoc />
    public override string? InstalledVersion
    {
        get
        {
            var marker = Path.Combine(TargetDirectory, VersionMarkerFile);
            try
            {
                return File.Exists(marker) ? File.ReadAllText(marker).Trim() : null;
            }
            catch (IOException)
            {
                return null;
            }
        }
    }

    /// <inheritdoc />
    public override bool IsInstalled() => File.Exists(Path.Combine(TargetDirectory, ExecutableRelativePath));

    /// <inheritdoc />
    public override async Task<Result> InstallAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct)
    {
        if (IsInstalled())
        {
            Report(progress, DisplayName + " zaten kurulu.", 100);
            return Result.Success();
        }

        Report(progress, DisplayName + " için indirme adresi alınıyor…", 0);
        var resolved = await ResolveDownloadAsync(ct);
        if (resolved.IsFailure)
        {
            return resolved.Error;
        }

        var download = resolved.Value;
        var downloads = Path.Combine(Paths.ToolsDir, "_downloads");
        var archive = Path.Combine(downloads, download.FileName);
        Report(progress, DisplayName + " " + download.Version + " indiriliyor…", 1);
        var downloadProgress = new Execution.InlineProgress<double>(p => Report(progress, DisplayName + " indiriliyor… %" + Math.Round(p), 1 + (p * 0.79)));
        var downloaded = await Downloader.DownloadAsync(download.Url, archive, download.Sha256, downloadProgress, ct);
        if (downloaded.IsFailure)
        {
            return downloaded.Error;
        }

        Report(progress, DisplayName + " kuruluyor…", 85);
        try
        {
            var installed = await InstallFromFileAsync(archive, ct);
            if (installed.IsFailure)
            {
                return installed.Error;
            }

            await File.WriteAllTextAsync(Path.Combine(TargetDirectory, VersionMarkerFile), download.Version, ct);
        }
        finally
        {
            TryDeleteFile(archive);
        }

        if (!IsInstalled())
        {
            return Error.Failure("agent.prerequisite_verify_failed", DisplayName + " kuruldu ama doğrulanamadı (" + ExecutableRelativePath + " yok).");
        }

        Report(progress, DisplayName + " hazır.", 100);
        return Result.Success();
    }

    /// <summary>İndirme adresini (ve biliniyorsa hash'i) çözer.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>İndirme bilgisi ya da hata.</returns>
    protected abstract Task<Result<PrerequisiteDownload>> ResolveDownloadAsync(CancellationToken ct);

    /// <summary>İndirilen dosyayı hedef klasöre kurar (varsayılan: zip aç + düzleştir + taşı).</summary>
    /// <param name="archivePath">İndirilen dosya.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    protected virtual async Task<Result> InstallFromFileAsync(string archivePath, CancellationToken ct)
    {
        var staging = Path.Combine(Paths.ToolsDir, "_staging", Id + "-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, staging, overwriteFiles: true), ct);
            var root = SingleRootOrSelf(staging);
            if (Directory.Exists(TargetDirectory))
            {
                Directory.Delete(TargetDirectory, recursive: true);
            }

            Directory.Move(root, TargetDirectory);
            return Result.Success();
        }
        catch (InvalidDataException ex)
        {
            return Error.Failure("agent.prerequisite_extract_failed", DisplayName + " arşivi açılamadı: " + ex.Message);
        }
        catch (IOException ex)
        {
            return Error.Failure("agent.prerequisite_extract_failed", DisplayName + " kurulamadı (dosya kullanımda olabilir): " + ex.Message);
        }
        finally
        {
            if (Directory.Exists(staging))
            {
                try
                {
                    Directory.Delete(staging, recursive: true);
                }
                catch (IOException)
                {
                    // Geçici klasör sonra temizlenir.
                }
            }
        }
    }

    /// <summary>Klasörde yalnızca tek alt klasör varsa onu, yoksa klasörün kendisini döner (zip içindeki üst klasörü düzleştirmek için).</summary>
    /// <param name="directory">Klasör.</param>
    /// <returns>Kök.</returns>
    protected static string SingleRootOrSelf(string directory)
    {
        var dirs = Directory.GetDirectories(directory);
        return dirs.Length == 1 && Directory.GetFiles(directory).Length == 0 ? dirs[0] : directory;
    }

    /// <summary><c>sha256sum</c> biçimli metinden (<c>hash  dosya</c>) ilgili dosyanın hash'ini çıkarır; dosya adı boşsa ilk hash.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="fileName">Dosya adı (isteğe bağlı).</param>
    /// <returns>Hash ya da <c>null</c>.</returns>
    public static string? ParseSha256(string? text, string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split([' ', '\t', '*'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || parts[0].Length != 64 || !parts[0].All(Uri.IsHexDigit))
            {
                continue;
            }

            if (string.IsNullOrEmpty(fileName) || (parts.Length > 1 && string.Equals(parts[^1], fileName, StringComparison.OrdinalIgnoreCase)))
            {
                return parts[0].ToLowerInvariant();
            }
        }

        return null;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Sonra temizlenir.
        }
    }
}
