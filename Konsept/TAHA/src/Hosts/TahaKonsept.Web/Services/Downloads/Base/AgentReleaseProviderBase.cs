using System.Text.Json;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Services.Downloads.Abstract;

namespace TahaKonsept.Web.Services.Downloads.Base;

/// <summary>
/// <see cref="IAgentReleaseProvider"/> ortak davranışı: <see cref="DownloadsDirectory"/> içindeki kurulum dosyasının
/// boyut/tarihini ve Velopack akışındaki (<see cref="FeedFileName"/>) en yeni sürümü okur. Önemli üyeler:
/// <see cref="GetCurrent"/>, <see cref="ReadLatestVersion"/> (en yüksek <c>Full</c> paket sürümü; bozuk akışta null).
/// Klasörün nerede olduğu somut sınıftadır.
/// </summary>
public abstract class AgentReleaseProviderBase : IAgentReleaseProvider
{
    /// <summary>Kurulum dosyasının adı (deploy/agent/pack-agent.ps1 üretir).</summary>
    public const string SetupFileName = "TahaKonsept.Agent-win-Setup.exe";

    /// <summary>Velopack sürüm akışı dosyası.</summary>
    public const string FeedFileName = "releases.win.json";

    /// <summary>İndirme klasörünün sitedeki göreli yolu.</summary>
    public const string PublicPath = "/downloads/";

    private readonly ILogger _logger;

    /// <summary>Günlükçüyü alır.</summary>
    /// <param name="logger">Günlükçü.</param>
    protected AgentReleaseProviderBase(ILogger logger) => _logger = logger;

    /// <summary>Kurulum dosyası ve akışın bulunduğu klasör (mutlak yol).</summary>
    protected abstract string DownloadsDirectory { get; }

    /// <inheritdoc />
    public AgentReleaseInfo GetCurrent()
    {
        var setup = new FileInfo(Path.Combine(DownloadsDirectory, SetupFileName));
        if (!setup.Exists)
        {
            _logger.LogWarning("Masaüstü programı kurulum dosyası bulunamadı: {Path}", setup.FullName);
            return new AgentReleaseInfo { IsAvailable = false, FileName = SetupFileName, DownloadUrl = PublicPath + SetupFileName };
        }

        var version = ReadLatestVersion(Path.Combine(DownloadsDirectory, FeedFileName));
        var url = PublicPath + SetupFileName + (version is null ? string.Empty : "?v=" + Uri.EscapeDataString(version));
        return new AgentReleaseInfo
        {
            IsAvailable = true,
            FileName = SetupFileName,
            DownloadUrl = url,
            Version = version,
            SizeBytes = setup.Length,
            PublishedAtUtc = new DateTimeOffset(setup.LastWriteTimeUtc, TimeSpan.Zero),
        };
    }

    /// <summary>
    /// Akıştaki en yüksek sürümü döner: önce <c>Type = Full</c> paketler, yoksa tüm paketler. Ön sürüm eki (<c>-beta</c>)
    /// karşılaştırmada yok sayılır. Dosya yoksa ya da okunamazsa null.
    /// </summary>
    /// <param name="feedPath">Akış dosyasının yolu.</param>
    protected string? ReadLatestVersion(string feedPath)
    {
        if (!File.Exists(feedPath))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(feedPath));
            if (!TryGetProperty(document.RootElement, "Assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var entries = assets.EnumerateArray()
                .Where(a => a.ValueKind == JsonValueKind.Object)
                .Select(a => (Version: ReadString(a, "Version"), Type: ReadString(a, "Type")))
                .Where(a => a.Version is not null && ParseVersion(a.Version) is not null)
                .ToList();
            var full = entries.Where(a => string.Equals(a.Type, "Full", StringComparison.OrdinalIgnoreCase)).ToList();
            return (full.Count > 0 ? full : entries)
                .OrderByDescending(a => ParseVersion(a.Version!))
                .Select(a => a.Version)
                .FirstOrDefault();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Masaüstü programı sürüm akışı okunamadı: {Path}", feedPath);
            return null;
        }
    }

    private static Version? ParseVersion(string value)
    {
        var core = value.Split('-', '+')[0];
        return System.Version.TryParse(core, out var parsed) ? parsed : null;
    }

    private static string? ReadString(JsonElement element, string name) =>
        TryGetProperty(element, name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
