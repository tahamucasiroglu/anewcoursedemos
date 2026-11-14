using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajanın aşama ilerleme raporu (<c>IAgentHub.StageProgress</c>).
/// </summary>
public sealed record StageProgressReport
{
    /// <summary>İlgili komut kimliği.</summary>
    public Guid CommandId { get; init; }

    /// <summary>Üretim işi kimliği.</summary>
    public Guid JobId { get; init; }

    /// <summary>Aşama.</summary>
    public PipelineStage Stage { get; init; }

    /// <summary>Sahne indeksi.</summary>
    public int? SceneIndex { get; init; }

    /// <summary>Yüzde (0-100).</summary>
    public double Percent { get; init; }

    /// <summary>Kısa durum mesajı (Türkçe).</summary>
    public string Message { get; init; } = string.Empty;
}
