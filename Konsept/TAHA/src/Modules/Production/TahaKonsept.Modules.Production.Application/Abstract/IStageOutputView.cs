using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Aşama işleyicilerinin üst aşama çıktılarına eriştiği görünüm. Gerçek yürütmede işin çalıştırmalarından okunur
/// (<c>JobStageOutputView</c>), yeniden kullanım planında benzetilir (<c>SimulatedStageOutputView</c>; değişen aşamaların çıktı hash'i
/// yapay). Önemli üyeler: <see cref="OutputHashOf"/> (hash zinciri), <see cref="OutputOf{T}"/>, <see cref="ArtifactsOf"/>, <see cref="ScenePlan"/>.
/// </summary>
public interface IStageOutputView
{
    /// <summary>Sahne planı (deterministik; her iki görünümde de kullanılabilir).</summary>
    ScenePlanOutput ScenePlan { get; }

    /// <summary>Aşamanın çıktı hash'i (henüz yoksa <c>null</c>).</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Hash.</returns>
    string? OutputHashOf(PipelineStage stage, int? sceneIndex = null);

    /// <summary>Aşamanın çıktısını okur.</summary>
    /// <typeparam name="T">Çıktı türü.</typeparam>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Çıktı ya da <c>null</c>.</returns>
    T? OutputOf<T>(PipelineStage stage, int? sceneIndex = null)
        where T : class;

    /// <summary>Aşamanın online üretilmiş artefaktları.</summary>
    /// <param name="stage">Aşama.</param>
    /// <param name="sceneIndex">Sahne.</param>
    /// <returns>Artefaktlar.</returns>
    IReadOnlyList<ArtifactDownload> ArtifactsOf(PipelineStage stage, int? sceneIndex = null);
}
