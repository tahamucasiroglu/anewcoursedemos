namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Üretim sırasındaki bir öğe: konsept, sıra, durum, ilerleme, aşama özetleri ve (varsa) iş/video kimlikleri.
/// </summary>
public sealed record QueueItemDto
{
    /// <summary>Öğe kimliği.</summary>
    public Guid Id { get; init; }

    /// <summary>Konsept.</summary>
    public Guid ConceptId { get; init; }

    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Sıra (küçük önce).</summary>
    public int Order { get; init; }

    /// <summary>Durum.</summary>
    public QueueItemStatus Status { get; init; }

    /// <summary>Etkin/son iş.</summary>
    public Guid? JobId { get; init; }

    /// <summary>Bitince oluşan video.</summary>
    public Guid? VideoId { get; init; }

    /// <summary>Genel ilerleme (0-100).</summary>
    public double Progress { get; init; }

    /// <summary>Durum mesajı (ör. "Ajan bekleniyor").</summary>
    public string? Message { get; init; }

    /// <summary>Şu anki aşama.</summary>
    public PipelineStage? CurrentStage { get; init; }

    /// <summary>Masaüstü programı bekleniyor mu.</summary>
    public bool WaitingForAgent { get; init; }

    /// <summary>Sürüm numarası (yeni sürümde artar).</summary>
    public int Version { get; init; } = 1;

    /// <summary>Yeni sürümse kaynak video.</summary>
    public Guid? ParentVideoId { get; init; }

    /// <summary>Şu ana kadarki maliyet ($).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Aşama özetleri (sıralı).</summary>
    public IReadOnlyList<StageSummaryDto> Stages { get; init; } = [];

    /// <summary>Eklenme zamanı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Başlama zamanı (UTC).</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>Bitiş zamanı (UTC).</summary>
    public DateTimeOffset? FinishedAt { get; init; }
}
