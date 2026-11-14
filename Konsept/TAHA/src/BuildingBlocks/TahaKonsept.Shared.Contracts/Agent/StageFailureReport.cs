using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajanın aşama hata raporu (<c>IAgentHub.StageFailed</c>).
/// </summary>
public sealed record StageFailureReport
{
    /// <summary>İlgili komut kimliği.</summary>
    public Guid CommandId { get; init; }

    /// <summary>Üretim işi kimliği.</summary>
    public Guid JobId { get; init; }

    /// <summary>Aşama.</summary>
    public PipelineStage Stage { get; init; }

    /// <summary>Sahne indeksi.</summary>
    public int? SceneIndex { get; init; }

    /// <summary>Hata kodu (ör. <c>agent.ffmpeg_failed</c>).</summary>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>Türkçe hata mesajı.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Teknik ayrıntı (stderr özeti, yığın izi).</summary>
    public string? Details { get; init; }

    /// <summary>Yeniden denenebilir mi.</summary>
    public bool Retryable { get; init; }

    /// <summary>İptal nedeniyle mi bitti.</summary>
    public bool Cancelled { get; init; }
}
