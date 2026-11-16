using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Domain.Rules;

/// <summary>Kurulum kuralları (spec 0009 Req 9, 10, 12).</summary>
public static class SetupRules
{
    /// <summary>Başarısız indirmede otomatik yeniden deneme sayısı (Req 12).</summary>
    public const int AutomaticRetries = 2;

    /// <summary>Gereken alan = kurulacak bileşenlerin indirme + açılmış boyutu × 1,2 (Req 10).</summary>
    /// <param name="components">Kurulacak bileşenler.</param>
    /// <returns>Bayt.</returns>
    public static long RequiredBytes(IEnumerable<SetupComponent> components)
    {
        ArgumentNullException.ThrowIfNull(components);
        var total = components.Sum(c => c.DownloadBytes + c.InstalledBytes);
        return (long)Math.Ceiling(total * 1.2);
    }

    /// <summary>Video klasörü tam yol olmalı; kurulum yeriyle aynı, onun içinde ya da onu içeren klasör olamaz (Req 9, AC-10).</summary>
    /// <param name="videoFolder">Seçilen klasör.</param>
    /// <param name="installRoot">Kurulum yeri.</param>
    /// <returns>Geçerli mi.</returns>
    public static bool IsValidVideoFolder(string? videoFolder, string installRoot)
    {
        ArgumentNullException.ThrowIfNull(installRoot);
        if (string.IsNullOrWhiteSpace(videoFolder) || !Path.IsPathFullyQualified(videoFolder))
        {
            return false;
        }

        var video = Normalize(videoFolder);
        var root = Normalize(installRoot);
        return !string.Equals(video, root, StringComparison.OrdinalIgnoreCase)
            && !video.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !root.StartsWith(video + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
