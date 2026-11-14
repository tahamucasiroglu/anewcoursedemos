namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Video ayrıntısında aşama başına maliyet ve zamanlama.
/// </summary>
public sealed record StageCostDto
{
    /// <summary>Aşama.</summary>
    public PipelineStage Stage { get; init; }

    /// <summary>Maliyet ($).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Süre (ms).</summary>
    public long DurationMs { get; init; }

    /// <summary>Giriş token'ı.</summary>
    public long InputTokens { get; init; }

    /// <summary>Çıkış token'ı.</summary>
    public long OutputTokens { get; init; }

    /// <summary>Önceki sürümden yeniden kullanıldı mı.</summary>
    public bool Reused { get; init; }

    /// <summary>Başlama (UTC).</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>Bitiş (UTC).</summary>
    public DateTimeOffset? FinishedAt { get; init; }
}
