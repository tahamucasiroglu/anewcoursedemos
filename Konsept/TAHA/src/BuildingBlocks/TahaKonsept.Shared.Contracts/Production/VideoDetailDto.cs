using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Video ayrıntısı (<c>GET api/production/videos/{id}</c>): sahneler (yazılış + okunuş), konsept metni, maliyet dökümü, aşama zamanları,
/// ayar anlık görüntüsü ve ajanın yerel sunucusundan oynatma/küçük resim adresleri (ajan çevrimdışıysa <c>null</c> + <see cref="PlaybackMessage"/>).
/// </summary>
public sealed record VideoDetailDto
{
    /// <summary>Video kimliği.</summary>
    public Guid Id { get; init; }

    /// <summary>Konsept.</summary>
    public Guid ConceptId { get; init; }

    /// <summary>Üreten iş.</summary>
    public Guid JobId { get; init; }

    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Biçim.</summary>
    public VideoFormat Format { get; init; }

    /// <summary>Genişlik (px).</summary>
    public int Width { get; init; }

    /// <summary>Yükseklik (px).</summary>
    public int Height { get; init; }

    /// <summary>Kare hızı.</summary>
    public int Fps { get; init; }

    /// <summary>Süre (sn).</summary>
    public double DurationSec { get; init; }

    /// <summary>Toplam maliyet ($).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Sürüm.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Kaynak video.</summary>
    public Guid? ParentVideoId { get; init; }

    /// <summary>Oluşturma (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Üretim başlangıcı (UTC).</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>Üretim bitişi (UTC).</summary>
    public DateTimeOffset? FinishedAt { get; init; }

    /// <summary>Üretimde kullanılan ayarlar.</summary>
    public ProductionSettingsDto Settings { get; init; } = new();

    /// <summary>Konsept metni (üretim anındaki anlık görüntü).</summary>
    public ConceptDocument Document { get; init; } = new();

    /// <summary>Sahneler.</summary>
    public IReadOnlyList<VideoSceneDto> Scenes { get; init; } = [];

    /// <summary>Aşama başına maliyet ve zamanlama.</summary>
    public IReadOnlyList<StageCostDto> CostBreakdown { get; init; } = [];

    /// <summary>Oynatma adresi (ajan yerel sunucusu).</summary>
    public string? PlaybackUrl { get; init; }

    /// <summary>Küçük resim adresi.</summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>Ajan çevrimiçi mi.</summary>
    public bool AgentOnline { get; init; }

    /// <summary>Oynatılamıyorsa nedeni (ör. "Masaüstü programı kapalı").</summary>
    public string? PlaybackMessage { get; init; }
}
