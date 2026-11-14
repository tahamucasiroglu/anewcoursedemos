using System.Text.Json;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Downloads.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Prerequisites.Concrete;

/// <summary>
/// Chrome Headless Shell (Chrome for Testing, Stable, win64) → <c>Tools/chrome-headless-shell/chrome-headless-shell.exe</c>.
/// Adres <c>last-known-good-versions-with-downloads.json</c>'dan çözülür (hash yayımlanmaz). Render motorları için gerekir.
/// </summary>
public sealed class ChromeHeadlessShellPrerequisite : DownloadablePrerequisiteBase
{
    /// <summary>Chrome for Testing sürüm uç noktası.</summary>
    public static readonly Uri VersionsUrl = new("https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions-with-downloads.json");

    /// <summary>Oluşturur.</summary>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="downloader">İndirici.</param>
    public ChromeHeadlessShellPrerequisite(IAgentPaths paths, IFileDownloader downloader)
        : base(paths, downloader)
    {
    }

    /// <inheritdoc />
    public override string Id => "chrome-headless-shell";

    /// <inheritdoc />
    public override string DisplayName => "Chrome Headless Shell";

    /// <inheritdoc />
    public override string Description => "Sahne kodunu (HTML/TSX) videoya çeviren başsız tarayıcı.";

    /// <inheritdoc />
    public override int Order => 30;

    /// <inheritdoc />
    protected override string TargetDirectoryName => "chrome-headless-shell";

    /// <inheritdoc />
    protected override string ExecutableRelativePath => "chrome-headless-shell.exe";

    /// <summary>JSON'dan Stable kanalın sürümünü ve win64 adresini çıkarır.</summary>
    /// <param name="json">Uç nokta içeriği.</param>
    /// <returns>(sürüm, adres) ya da <c>null</c>.</returns>
    public static (string Version, Uri Url)? ParseStableWin64(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("channels", out var channels)
                || !channels.TryGetProperty("Stable", out var stable)
                || !stable.TryGetProperty("downloads", out var downloads)
                || !downloads.TryGetProperty("chrome-headless-shell", out var shells))
            {
                return null;
            }

            var version = stable.TryGetProperty("version", out var v) ? v.GetString() ?? "stable" : "stable";
            foreach (var item in shells.EnumerateArray())
            {
                if (item.TryGetProperty("platform", out var platform) && platform.GetString() == "win64"
                    && item.TryGetProperty("url", out var url) && Uri.TryCreate(url.GetString(), UriKind.Absolute, out var uri))
                {
                    return (version, uri);
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
        var json = await Downloader.GetStringAsync(VersionsUrl, ct);
        if (json.IsFailure)
        {
            return json.Error;
        }

        var parsed = ParseStableWin64(json.Value);
        return parsed is { } found
            ? new PrerequisiteDownload(found.Url, "chrome-headless-shell-win64.zip", null, found.Version)
            : Error.Failure("agent.prerequisite_resolve_failed", "Chrome Headless Shell indirme adresi bulunamadı.");
    }
}
