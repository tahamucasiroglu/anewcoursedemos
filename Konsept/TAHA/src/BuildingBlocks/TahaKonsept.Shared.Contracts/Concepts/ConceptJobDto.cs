namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Arka plan konsept üretim işinin anlık durumu (<c>GET api/concepts/jobs/current</c>; Web 2 sn'de bir sorgular):
/// toplam / biten / başarısız sayıları, şu an üretilen başlık, maliyet, mesaj ve üretilen konsept kimlikleri.
/// </summary>
public sealed record ConceptJobDto
{
    /// <summary>İş kimliği (iş yoksa <see cref="Guid.Empty"/>).</summary>
    public Guid Id { get; init; }

    /// <summary>İş türü.</summary>
    public ConceptJobKind Kind { get; init; }

    /// <summary>Durum.</summary>
    public ConceptJobStatus Status { get; init; }

    /// <summary>Toplam öğe sayısı.</summary>
    public int Total { get; init; }

    /// <summary>Başarıyla üretilen sayısı.</summary>
    public int Completed { get; init; }

    /// <summary>Başarısız öğe sayısı.</summary>
    public int Failed { get; init; }

    /// <summary>Bu işten sonra sırada bekleyen iş sayısı.</summary>
    public int QueuedJobs { get; init; }

    /// <summary>Şu an üretilen başlık.</summary>
    public string? CurrentTitle { get; init; }

    /// <summary>Kullanıcıya gösterilecek son mesaj (ör. hata nedeni).</summary>
    public string? Message { get; init; }

    /// <summary>Toplam maliyet ($).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Üretilen konsept kimlikleri.</summary>
    public IReadOnlyList<Guid> ProducedIds { get; init; } = [];

    /// <summary>Kuyruğa alınma anı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Başlama anı (UTC).</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>Bitiş anı (UTC).</summary>
    public DateTimeOffset? FinishedAt { get; init; }
}
