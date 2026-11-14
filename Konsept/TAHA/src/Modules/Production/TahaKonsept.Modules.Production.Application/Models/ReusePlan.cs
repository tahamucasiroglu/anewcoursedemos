using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Models;

/// <summary>
/// Yeni sürüm için yeniden kullanım planı: karar listesi ve yeniden çalışacak / yeniden kullanılacak aşamalar.
/// </summary>
public sealed record ReusePlan
{
    /// <summary>Kararlar (hat sırasıyla).</summary>
    public IReadOnlyList<StageDecision> Decisions { get; init; } = [];

    /// <summary>Yeniden çalışacak aşamalar.</summary>
    public IReadOnlyList<PipelineStage> RerunStages => Decisions.Where(d => !d.Reuse).Select(d => d.Stage).Distinct().ToList();

    /// <summary>Tamamen yeniden kullanılacak aşamalar.</summary>
    public IReadOnlyList<PipelineStage> ReusedStages => Decisions.GroupBy(d => d.Stage).Where(g => g.All(d => d.Reuse)).Select(g => g.Key).ToList();
}
