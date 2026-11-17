using System.Text.Json;
using System.Text.RegularExpressions;
using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Domain.Rules;

/// <summary>
/// Güncelleme kuralları (spec 0023 Req 1, 6, 8, 9): denetim aralığı, yeni sürümün ilk açılış süresi, sürüm karşılaştırması ve imzalı listenin
/// ayrıştırılması. Liste yalnızca imzası doğrulandıktan sonra ayrıştırılır.
/// </summary>
public static partial class UpdateRules
{
    /// <summary>Exe'nin kurulu adı (<c>%LOCALAPPDATA%\anewconcept\app</c> altında).</summary>
    public const string InstalledFileName = "anewconcept-ajan.exe";

    /// <summary>Denetim aralığı (Req 1).</summary>
    public static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    /// <summary>Yeni sürümün ilk açılışında API'ye bağlanması için süre (Req 9).</summary>
    public static readonly TimeSpan CommitTimeout = TimeSpan.FromSeconds(60);

    /// <summary>"Üretiliyor" iş varken güncellemenin yeniden sorulma aralığı (Req 3).</summary>
    public static readonly TimeSpan ProductionRetryInterval = TimeSpan.FromSeconds(15);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Aday sürüm kurulu sürümden yeni mi (eski ya da aynıysa tekrar oynatma — Req 8).</summary>
    /// <param name="candidate">Listedeki sürüm.</param>
    /// <param name="installed">Kurulu sürüm.</param>
    /// <returns>Yeniyse <c>true</c>; biri geçersizse <c>false</c>.</returns>
    public static bool IsNewer(string? candidate, string? installed) =>
        TryParse(candidate, out var c) && TryParse(installed, out var i) && c > i;

    /// <summary>İmzası doğrulanmış listeyi ayrıştırır ve alanlarını denetler.</summary>
    /// <param name="json">Liste (UTF-8 JSON).</param>
    /// <returns>Liste; biçim geçersizse <c>null</c>.</returns>
    public static UpdateManifest? Parse(byte[] json)
    {
        ArgumentNullException.ThrowIfNull(json);
        UpdateManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<UpdateManifest>(json, Json);
        }
        catch (JsonException)
        {
            return null;
        }

        return manifest is not null
            && TryParse(manifest.Version, out _)
            && manifest.FileName is not null && PlainFileName().IsMatch(manifest.FileName)
            && manifest.Sha256 is not null && Sha256Hex().IsMatch(manifest.Sha256)
            && manifest.Size > 0
                ? manifest with { Sha256 = manifest.Sha256.ToLowerInvariant() }
                : null;
    }

    /// <summary>Gereken disk alanı: exe boyutu × 1,2 (0009 Req 10 ile aynı pay).</summary>
    /// <param name="manifest">Liste.</param>
    /// <returns>Bayt.</returns>
    public static long RequiredBytes(UpdateManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return (long)Math.Ceiling(manifest.Size * 1.2);
    }

    private static bool TryParse(string? text, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var plus = text.IndexOf('+', StringComparison.Ordinal);
        var core = plus < 0 ? text : text[..plus];
        if (!NumericVersion().IsMatch(core) || !Version.TryParse(core, out var parsed))
        {
            return false;
        }

        version = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0), Math.Max(parsed.Revision, 0));
        return true;
    }

    [GeneratedRegex(@"^\d+\.\d+(\.\d+){0,2}$")]
    private static partial Regex NumericVersion();

    [GeneratedRegex(@"^[A-Za-z0-9._-]+\.exe$")]
    private static partial Regex PlainFileName();

    [GeneratedRegex("^[0-9A-Fa-f]{64}$")]
    private static partial Regex Sha256Hex();
}
