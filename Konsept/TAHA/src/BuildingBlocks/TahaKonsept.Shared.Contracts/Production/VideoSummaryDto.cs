using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Üretilen video listesi öğesi (<c>GET api/production/videos?take=</c>).
/// </summary>
public sealed record VideoSummaryDto
{
    /// <summary>Video kimliği (ajandaki dosya kimliği ile aynı).</summary>
    public Guid Id { get; init; }

    /// <summary>Konsept.</summary>
    public Guid ConceptId { get; init; }

    /// <summary>Üreten iş.</summary>
    public Guid JobId { get; init; }

    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Biçim.</summary>
    public VideoFormat Format { get; init; }

    /// <summary>Süre (sn).</summary>
    public double DurationSec { get; init; }

    /// <summary>Toplam maliyet ($).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Sürüm.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Kaynak video (yeni sürümse).</summary>
    public Guid? ParentVideoId { get; init; }

    /// <summary>Oluşturma (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
