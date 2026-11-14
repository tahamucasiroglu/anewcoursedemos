namespace TahaKonsept.Agent.Core.Videos;

/// <summary>Yerel video dizinindeki kayıt: video kimliği → son dosya ve küçük resim.</summary>
public sealed record VideoEntry
{
    /// <summary>Video kimliği (sunucudaki ile aynı).</summary>
    public Guid VideoId { get; init; }

    /// <summary>Üretim işi kimliği (çalışma alanı klasörü).</summary>
    public Guid JobId { get; init; }

    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Son video dosyasının tam yolu.</summary>
    public string VideoPath { get; init; } = string.Empty;

    /// <summary>Küçük resim yolu (varsa).</summary>
    public string? ThumbnailPath { get; init; }

    /// <summary>Süre (sn; biliniyorsa).</summary>
    public double? DurationSec { get; init; }

    /// <summary>Boyut (bayt).</summary>
    public long SizeBytes { get; init; }

    /// <summary>Oluşturulma zamanı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }
}
