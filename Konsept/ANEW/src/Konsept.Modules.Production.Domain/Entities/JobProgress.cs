using Konsept.Modules.Production.Domain.Enums;

namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>
/// İşin hat ilerlemesi (spec 0017 Req 6, 11, 16, 18): aşama durumları (sırayla), o ana kadarki toplam maliyet (USD), sade hata açıklaması,
/// ilk başlangıç ve bitiş zamanı (UTC). İş satırında <c>jsonb</c> olarak saklanır.
/// </summary>
public sealed record JobProgress
{
    /// <summary>Gets aşamalar.</summary>
    public IReadOnlyList<StageProgress> Stages { get; init; } = [];

    /// <summary>Gets toplam maliyet (USD).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Gets sade hata açıklaması ya da <c>null</c>.</summary>
    public string? Error { get; init; }

    /// <summary>Gets a value indicating whether render düzeltme çağrısı bu işte kullanıldı (iş başına en fazla 1, Req 12).</summary>
    public bool FixUsed { get; init; }

    /// <summary>Gets birleştirmesi süren dosyanın adı (spec 0020 Req 10: Sürümü kaldır yarım dosyayı siler); yoksa <c>null</c>.</summary>
    public string? PendingFile { get; init; }

    /// <summary>Gets ilk başlangıç (UTC).</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>Gets bitiş (UTC).</summary>
    public DateTimeOffset? EndedAt { get; init; }

    /// <summary>Bir aşamanın durumunu değiştirir.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="state">Yeni durum.</param>
    /// <param name="device">Aygıt.</param>
    /// <returns>Yeni ilerleme.</returns>
    public JobProgress With(PipelineStage stage, StageState state, string? device = null) => this with
    {
        Stages = [.. Stages.Select(s => s.Stage == stage ? new StageProgress(stage, state, device ?? s.Device) : s)],
    };

    /// <summary>Aşamanın durumu.</summary>
    /// <param name="stage">Aşama.</param>
    /// <returns>Durum (yoksa bekliyor).</returns>
    public StageState StateOf(PipelineStage stage) => Stages.FirstOrDefault(s => s.Stage == stage)?.State ?? StageState.Pending;
}
