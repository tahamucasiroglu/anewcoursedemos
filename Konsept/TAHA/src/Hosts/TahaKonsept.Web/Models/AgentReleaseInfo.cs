using System.Globalization;

namespace TahaKonsept.Web.Models;

/// <summary>
/// Sunucuda yayınlanan masaüstü programı (Windows kurulum dosyası) bilgisi; Başlangıç ve Ayarlar › Masaüstü programı
/// kartındaki indirme düğmesi bundan çizilir. Önemli alanlar: <see cref="IsAvailable"/> (dosya sunucuda var mı),
/// <see cref="Version"/> (Velopack akışı <c>releases.win.json</c>'daki en yeni sürüm), <see cref="SizeBytes"/> (dosyanın
/// gerçek boyutu), <see cref="DownloadUrl"/> (sürüme göre önbellek kıran göreli adres), <see cref="SizeText"/> (tr-TR biçimli boyut).
/// </summary>
public sealed record AgentReleaseInfo
{
    /// <summary>Kurulum dosyası sunucuda var mı.</summary>
    public bool IsAvailable { get; init; }

    /// <summary>Kurulum dosyasının adı.</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>Kök göreli indirme adresi (ör. <c>/downloads/TahaKonsept.Agent-win-Setup.exe?v=0.2.4</c>).</summary>
    public string DownloadUrl { get; init; } = string.Empty;

    /// <summary>En yeni sürüm (akış okunamazsa null).</summary>
    public string? Version { get; init; }

    /// <summary>Dosya boyutu (bayt; dosya yoksa 0).</summary>
    public long SizeBytes { get; init; }

    /// <summary>Dosyanın son yazılma zamanı (UTC; dosya yoksa null).</summary>
    public DateTimeOffset? PublishedAtUtc { get; init; }

    /// <summary>Okunur boyut (ör. <c>86,9 MB</c>).</summary>
    public string SizeText => FormatSize(SizeBytes);

    /// <summary>Bayt değerini tr-TR biçiminde KB/MB/GB metnine çevirir.</summary>
    /// <param name="bytes">Bayt.</param>
    public static string FormatSize(long bytes)
    {
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        return bytes switch
        {
            >= 1L << 30 => (bytes / (double)(1L << 30)).ToString("0.0", culture) + " GB",
            >= 1L << 20 => (bytes / (double)(1L << 20)).ToString("0.0", culture) + " MB",
            >= 1L << 10 => (bytes / (double)(1L << 10)).ToString("0", culture) + " KB",
            _ => bytes.ToString(culture) + " B",
        };
    }
}
