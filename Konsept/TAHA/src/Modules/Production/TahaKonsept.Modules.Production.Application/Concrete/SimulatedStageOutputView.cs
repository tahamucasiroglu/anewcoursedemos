using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Yeniden kullanım planı için benzetim görünümü: planlayıcı her aşamayı işledikçe çıktı hash'ini (yeniden kullanılırsa kaynak işinkini,
/// değişirse gerçek ya da yapay hash'i) ve varsa çıktı JSON'unu <see cref="Set"/> ile yazar; sonraki aşamalar buradan okur.
/// </summary>
public sealed class SimulatedStageOutputView : StageOutputViewBase
{
    private readonly Dictionary<(PipelineStage Stage, int? Scene), (string? Json, string Hash, string? Artifacts)> _items = [];

    /// <summary>Oluşturur.</summary>
    /// <param name="planFallback">Plan üreticisi.</param>
    public SimulatedStageOutputView(Func<ScenePlanOutput> planFallback)
        : base(planFallback)
    {
    }

    /// <summary>Aşama çıktısını kaydeder.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <param name="outputHash">Çıktı hash'i.</param>
    /// <param name="outputJson">Çıktı JSON'u.</param>
    /// <param name="artifactsJson">Artefakt JSON'u.</param>
    public void Set(PipelineStage stage, int? sceneIndex, string outputHash, string? outputJson, string? artifactsJson) =>
        _items[(stage, sceneIndex)] = (outputJson, outputHash, artifactsJson);

    /// <summary>Çıktı JSON'u.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>JSON.</returns>
    protected override string? FindOutputJson(PipelineStage stage, int? sceneIndex) =>
        _items.TryGetValue((stage, sceneIndex), out var item) ? item.Json : null;

    /// <summary>Çıktı hash'i.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Hash.</returns>
    protected override string? FindOutputHash(PipelineStage stage, int? sceneIndex) =>
        _items.TryGetValue((stage, sceneIndex), out var item) ? item.Hash : null;

    /// <summary>Artefakt JSON'u.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>JSON.</returns>
    protected override string? FindArtifactsJson(PipelineStage stage, int? sceneIndex) =>
        _items.TryGetValue((stage, sceneIndex), out var item) ? item.Artifacts : null;
}
