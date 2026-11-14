using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Downloads.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Prerequisites.Concrete;

/// <summary>
/// Astral <c>uv</c> (GitHub son sürüm, <c>uv-x86_64-pc-windows-msvc.zip</c> + <c>.sha256</c>) → <c>Tools/uv/uv.exe</c>.
/// Python motorları (Chatterbox, XTTS…) için gömülü Python ve venv yönetimi. İsteğe bağlı.
/// </summary>
public sealed class UvPrerequisite : DownloadablePrerequisiteBase
{
    private const string AssetName = "uv-x86_64-pc-windows-msvc.zip";
    private static readonly Uri ReleaseBase = new("https://github.com/astral-sh/uv/releases/latest/download/");

    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="downloader">İndirici.</param>
    public UvPrerequisite(IAgentPaths paths, IFileDownloader downloader)
        : base(paths, downloader)
    {
    }

    /// <inheritdoc />
    public override string Id => "uv";

    /// <inheritdoc />
    public override string DisplayName => "uv (Python yöneticisi)";

    /// <inheritdoc />
    public override string Description => "Python tabanlı ses motorları için gömülü Python ve sanal ortam yönetimi.";

    /// <inheritdoc />
    public override bool IsOptional => true;

    /// <inheritdoc />
    public override string Group => PythonGroup;

    /// <inheritdoc />
    public override int Order => 40;

    /// <inheritdoc />
    protected override string TargetDirectoryName => "uv";

    /// <inheritdoc />
    protected override string ExecutableRelativePath => "uv.exe";

    /// <inheritdoc />
    protected override async Task<Result<PrerequisiteDownload>> ResolveDownloadAsync(CancellationToken ct)
    {
        var hash = await Downloader.GetStringAsync(new Uri(ReleaseBase, AssetName + ".sha256"), ct);
        return new PrerequisiteDownload(new Uri(ReleaseBase, AssetName), AssetName, hash.IsSuccess ? ParseSha256(hash.Value) : null, "latest");
    }
}
