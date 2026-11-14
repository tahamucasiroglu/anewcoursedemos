namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Konsept liste öğesi (<c>GET api/concepts?status=</c>, <c>GET api/concepts/deleted</c>): başlık, özet, durum, kaynak,
/// sahne sayısı, tahmini süre, maliyet ve zaman damgaları. Belgenin tamamı için <see cref="ConceptDetailDto"/>.
/// </summary>
public sealed record ConceptSummaryDto
{
    /// <summary>Konsept kimliği.</summary>
    public Guid Id { get; init; }

    /// <summary>Üretildiği profil (kanal).</summary>
    public Guid? ProfileId { get; init; }

    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Kısa özet.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Durum.</summary>
    public ConceptStatus Status { get; init; }

    /// <summary>Kaynak (konu / fikir / düzeltme).</summary>
    public ConceptOrigin Origin { get; init; }

    /// <summary>Sahne sayısı.</summary>
    public int SceneCount { get; init; }

    /// <summary>Sahne süre ipuçlarının toplamı (saniye; yoksa hedef süre).</summary>
    public int EstimatedDurationSec { get; init; }

    /// <summary>Etiketler.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Toplam LLM maliyeti ($; üretim + düzeltmeler + kelime denetimi).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Son güncellenme anı (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>Onaylanma anı (UTC).</summary>
    public DateTimeOffset? ApprovedAt { get; init; }

    /// <summary>Silinme anı (UTC).</summary>
    public DateTimeOffset? DeletedAt { get; init; }
}
