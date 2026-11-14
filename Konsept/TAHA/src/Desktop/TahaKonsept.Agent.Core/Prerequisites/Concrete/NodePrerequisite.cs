using System.Text.Json;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Downloads.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Prerequisites.Concrete;

/// <summary>
/// Portable Node.js 22 LTS (win-x64 zip) → <c>Tools/node</c>. Sürüm <c>nodejs.org/dist/index.json</c>'dan (en yeni v22),
/// hash <c>SHASUMS256.txt</c>'den; ulaşılamazsa sabit yedek sürüm (hash'siz) kullanılır. Video motorları (HyperFrames vb.) için gerekir.
/// </summary>
public sealed class NodePrerequisite : DownloadablePrerequisiteBase
{
    /// <summary>Ana sürüm.</summary>
    public const string MajorVersion = "v22.";

    /// <summary>Dizin JSON'u alınamazsa kullanılacak sürüm.</summary>
    public const string FallbackVersion = "v22.20.0";

    private static readonly Uri DistBase = new("https://nodejs.org/dist/");

    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="downloader">İndirici.</param>
    public NodePrerequisite(IAgentPaths paths, IFileDownloader downloader)
        : base(paths, downloader)
    {
    }

    /// <inheritdoc />
    public override string Id => "node";

    /// <inheritdoc />
    public override string DisplayName => "Node.js 22 LTS";

    /// <inheritdoc />
    public override string Description => "Video motorları (HyperFrames, Remotion, Revideo) için taşınabilir Node.js.";

    /// <inheritdoc />
    public override int Order => 10;

    /// <inheritdoc />
    protected override string TargetDirectoryName => "node";

    /// <inheritdoc />
    protected override string ExecutableRelativePath => "node.exe";

    /// <summary><c>index.json</c> içeriğinden en yeni v22 sürümünü seçer.</summary>
    /// <param name="indexJson">JSON (en yeni önce sıralı dizi).</param>
    /// <returns>Sürüm (ör. <c>v22.21.1</c>) ya da <c>null</c>.</returns>
    public static string? PickLatestVersion(string indexJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(indexJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                if (entry.TryGetProperty("version", out var version)
                    && version.GetString() is { } text
                    && text.StartsWith(MajorVersion, StringComparison.Ordinal))
                {
                    return text;
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task<Result<PrerequisiteDownload>> ResolveDownloadAsync(CancellationToken ct)
    {
        var index = await Downloader.GetStringAsync(new Uri(DistBase, "index.json"), ct);
        var version = (index.IsSuccess ? PickLatestVersion(index.Value) : null) ?? FallbackVersion;
        var fileName = "node-" + version + "-win-x64.zip";
        var sums = await Downloader.GetStringAsync(new Uri(DistBase, version + "/SHASUMS256.txt"), ct);
        var sha = sums.IsSuccess ? ParseSha256(sums.Value, fileName) : null;
        return new PrerequisiteDownload(new Uri(DistBase, version + "/" + fileName), fileName, sha, version);
    }
}
