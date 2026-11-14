using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Downloads.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Prerequisites.Concrete;

/// <summary>
/// FFmpeg (gyan.dev release essentials zip) → <c>Tools/ffmpeg/bin/ffmpeg.exe</c>. Hash <c>.sha256</c> dosyasından, sürüm
/// <c>release-version</c>'dan okunur. Birleştirme, altyazı, ilerleme çubuğu ve ses işlemleri için gerekir.
/// </summary>
public sealed class FfmpegPrerequisite : DownloadablePrerequisiteBase
{
    /// <summary>Paket adresi.</summary>
    public static readonly Uri PackageUrl = new("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip");

    private static readonly Uri HashUrl = new("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip.sha256");
    private static readonly Uri VersionUrl = new("https://www.gyan.dev/ffmpeg/builds/release-version");

    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="downloader">İndirici.</param>
    public FfmpegPrerequisite(IAgentPaths paths, IFileDownloader downloader)
        : base(paths, downloader)
    {
    }

    /// <inheritdoc />
    public override string Id => "ffmpeg";

    /// <inheritdoc />
    public override string DisplayName => "FFmpeg";

    /// <inheritdoc />
    public override string Description => "Sahneleri birleştirme, altyazı ve ilerleme çubuğu basma, ses işleme.";

    /// <inheritdoc />
    public override int Order => 20;

    /// <inheritdoc />
    protected override string TargetDirectoryName => "ffmpeg";

    /// <inheritdoc />
    protected override string ExecutableRelativePath => Path.Combine("bin", "ffmpeg.exe");

    /// <inheritdoc />
    protected override async Task<Result<PrerequisiteDownload>> ResolveDownloadAsync(CancellationToken ct)
    {
        var hash = await Downloader.GetStringAsync(HashUrl, ct);
        var version = await Downloader.GetStringAsync(VersionUrl, ct);
        return new PrerequisiteDownload(
            PackageUrl,
            "ffmpeg-release-essentials.zip",
            hash.IsSuccess ? ParseSha256(hash.Value) : null,
            version.IsSuccess && !string.IsNullOrWhiteSpace(version.Value) ? version.Value.Trim() : "release");
    }
}
