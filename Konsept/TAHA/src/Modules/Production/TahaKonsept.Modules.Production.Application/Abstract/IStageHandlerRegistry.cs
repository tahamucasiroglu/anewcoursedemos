using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Aşama → işleyici sözlüğü (DI'daki tüm <see cref="IStageHandler"/> kayıtlarından).
/// </summary>
public interface IStageHandlerRegistry
{
    /// <summary>Tüm işleyiciler (hat sırasıyla).</summary>
    IReadOnlyList<IStageHandler> All { get; }

    /// <summary>Aşamanın işleyicisi.</summary>
    /// <param name="stage">Aşama.</param>
    /// <returns>Sonuç.</returns>
    IStageHandler? Find(PipelineStage stage);
}
