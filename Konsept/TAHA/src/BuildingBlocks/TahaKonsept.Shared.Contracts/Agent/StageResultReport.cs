using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajanın aşama tamamlama raporu (<c>IAgentHub.StageCompleted</c>): çıktı artefaktları, süre ve maliyet (yerelde 0).
/// </summary>
public sealed record StageResultReport
{
    /// <summary>İlgili komut kimliği.</summary>
    public Guid CommandId { get; init; }

    /// <summary>Üretim işi kimliği.</summary>
    public Guid JobId { get; init; }

    /// <summary>Aşama.</summary>
    public PipelineStage Stage { get; init; }

    /// <summary>Sahne indeksi.</summary>
    public int? SceneIndex { get; init; }

    /// <summary>Aşama çıktı JSON'u (opsiyonel, ör. hizalama zamanları).</summary>
    public string? OutputJson { get; init; }

    /// <summary>Üretilen artefaktlar.</summary>
    public IReadOnlyList<ProducedArtifact> Artifacts { get; init; } = [];

    /// <summary>Çalışma süresi (ms).</summary>
    public long DurationMs { get; init; }

    /// <summary>Maliyet (USD; yerel üretimde 0).</summary>
    public decimal CostUsd { get; init; }
}
