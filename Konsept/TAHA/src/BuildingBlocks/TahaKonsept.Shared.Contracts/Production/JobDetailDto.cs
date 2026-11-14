namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Üretim işinin ayrıntısı (<c>GET api/production/jobs/{id}</c>): ayar anlık görüntüsü, çözünürlük ve tüm aşama çalıştırmaları.
/// </summary>
public sealed record JobDetailDto
{
    /// <summary>İş kimliği.</summary>
    public Guid Id { get; init; }

    /// <summary>Sıra öğesi.</summary>
    public Guid QueueItemId { get; init; }

    /// <summary>Konsept.</summary>
    public Guid ConceptId { get; init; }

    /// <summary>Başlık.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Durum.</summary>
    public JobStatus Status { get; init; }

    /// <summary>Durum mesajı.</summary>
    public string? StatusMessage { get; init; }

    /// <summary>Ajan bekleniyor mu.</summary>
    public bool WaitingForAgent { get; init; }

    /// <summary>Oluşacak videonun kimliği (ajan dosya adı/dizini için önceden atanır).</summary>
    public Guid VideoId { get; init; }

    /// <summary>Yeni sürümde çıktıları yeniden kullanılan iş.</summary>
    public Guid? BaseJobId { get; init; }

    /// <summary>Sürüm.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Genişlik (px).</summary>
    public int Width { get; init; }

    /// <summary>Yükseklik (px).</summary>
    public int Height { get; init; }

    /// <summary>Kare hızı.</summary>
    public int Fps { get; init; }

    /// <summary>Ayar anlık görüntüsü.</summary>
    public ProductionSettingsDto Settings { get; init; } = new();

    /// <summary>Aşama çalıştırmaları (hat sırasıyla).</summary>
    public IReadOnlyList<StageRunDto> Runs { get; init; } = [];

    /// <summary>Toplam maliyet ($).</summary>
    public decimal TotalCostUsd { get; init; }

    /// <summary>Oluşturma (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Başlama (UTC).</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>Bitiş (UTC).</summary>
    public DateTimeOffset? FinishedAt { get; init; }
}
