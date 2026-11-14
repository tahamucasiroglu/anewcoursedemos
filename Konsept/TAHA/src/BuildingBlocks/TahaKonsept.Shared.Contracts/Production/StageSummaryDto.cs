namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Sıra öğesinde bir aşamanın özet ilerlemesi (sahne bazlı aşamalarda sahneler toplanır).
/// </summary>
public sealed record StageSummaryDto
{
    /// <summary>Aşama.</summary>
    public PipelineStage Stage { get; init; }

    /// <summary>Durum (sahne bazlı aşamalarda toplu durum).</summary>
    public StageStatus Status { get; init; }

    /// <summary>Çalıştığı yer.</summary>
    public StageLocation Location { get; init; }

    /// <summary>İlerleme (0-100).</summary>
    public double Progress { get; init; }

    /// <summary>Son durum mesajı.</summary>
    public string? Message { get; init; }

    /// <summary>Sahne bazlı aşamada toplam sahne (diğerlerinde 0).</summary>
    public int ScenesTotal { get; init; }

    /// <summary>Biten sahne sayısı.</summary>
    public int ScenesDone { get; init; }

    /// <summary>Önceki sürümden yeniden kullanıldı mı.</summary>
    public bool Reused { get; init; }

    /// <summary>Toplam süre (ms).</summary>
    public long DurationMs { get; init; }

    /// <summary>Maliyet ($).</summary>
    public decimal CostUsd { get; init; }
}
