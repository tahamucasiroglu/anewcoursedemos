using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IStageOutputView"/> ortak davranışı: çıktı JSON'u, hash ve artefaktlar somut sınıfın <see cref="FindOutputJson"/> /
/// <see cref="FindOutputHash"/> / <see cref="FindArtifactsJson"/> uygulamalarından okunur; sahne planı önce ScenePlan çıktısından, yoksa
/// verilen deterministik planlayıcı fonksiyonundan alınır (bir kez hesaplanır).
/// </summary>
public abstract class StageOutputViewBase : IStageOutputView
{
    private readonly Lazy<ScenePlanOutput> _scenePlan;

    /// <summary>Oluşturur.</summary>
    /// <param name="planFallback">ScenePlan çıktısı yoksa planı üreten fonksiyon.</param>
    protected StageOutputViewBase(Func<ScenePlanOutput> planFallback)
    {
        ArgumentNullException.ThrowIfNull(planFallback);
        _scenePlan = new Lazy<ScenePlanOutput>(() => OutputOf<ScenePlanOutput>(PipelineStage.ScenePlan) ?? planFallback());
    }

    /// <summary>Sahne planı.</summary>
    public ScenePlanOutput ScenePlan => _scenePlan.Value;

    /// <summary>Çıktı hash'i.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Hash.</returns>
    public string? OutputHashOf(PipelineStage stage, int? sceneIndex = null) => FindOutputHash(stage, sceneIndex);

    /// <summary>Çıktıyı okur.</summary>
    /// <typeparam name="T">Tür.</typeparam>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Çıktı.</returns>
    public T? OutputOf<T>(PipelineStage stage, int? sceneIndex = null)
        where T : class => ProductionJson.Read<T>(FindOutputJson(stage, sceneIndex));

    /// <summary>Artefaktlar.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Artefaktlar.</returns>
    public IReadOnlyList<ArtifactDownload> ArtifactsOf(PipelineStage stage, int? sceneIndex = null) =>
        ProductionJson.Read<List<ArtifactDownload>>(FindArtifactsJson(stage, sceneIndex)) ?? [];

    /// <summary>Çıktı JSON'unu bulur.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>JSON ya da <c>null</c>.</returns>
    protected abstract string? FindOutputJson(PipelineStage stage, int? sceneIndex);

    /// <summary>Çıktı hash'ini bulur.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Hash ya da <c>null</c>.</returns>
    protected abstract string? FindOutputHash(PipelineStage stage, int? sceneIndex);

    /// <summary>Artefakt JSON'unu bulur.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>JSON ya da <c>null</c>.</returns>
    protected abstract string? FindArtifactsJson(PipelineStage stage, int? sceneIndex);
}
