using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Gerçek yürütme görünümü: bir işin biten (tamamlanan/yeniden kullanılan) çalıştırmalarından çıktı, hash ve artefakt okur.
/// Çalıştırma listesine canlı bakar; aynı adımda biten aşamanın çıktısı hemen görünür.
/// </summary>
public sealed class JobStageOutputView : StageOutputViewBase
{
    private readonly IReadOnlyList<StageRun> _runs;

    /// <summary>Oluşturur.</summary>
    /// <param name="runs">İşin çalıştırmaları.</param>
    /// <param name="planFallback">Plan yedeği.</param>
    public JobStageOutputView(IReadOnlyList<StageRun> runs, Func<ScenePlanOutput> planFallback)
        : base(planFallback)
    {
        _runs = runs ?? throw new ArgumentNullException(nameof(runs));
    }

    /// <summary>Çıktı JSON'u.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>JSON.</returns>
    protected override string? FindOutputJson(PipelineStage stage, int? sceneIndex) => Find(stage, sceneIndex)?.OutputJson;

    /// <summary>Çıktı hash'i.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Hash.</returns>
    protected override string? FindOutputHash(PipelineStage stage, int? sceneIndex) => Find(stage, sceneIndex)?.OutputHash;

    /// <summary>Artefakt JSON'u.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>JSON.</returns>
    protected override string? FindArtifactsJson(PipelineStage stage, int? sceneIndex) => Find(stage, sceneIndex)?.ArtifactsJson;

    private StageRun? Find(PipelineStage stage, int? sceneIndex) =>
        _runs.FirstOrDefault(r => r.Stage == stage && r.SceneIndex == sceneIndex && r.IsDone);
}
