using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IStageHandlerRegistry"/> ortak davranışı: verilen işleyicileri aşamaya göre sözlüğe koyar (aynı aşama için son kayıt kazanır)
/// ve hat sırasına dizer.
/// </summary>
public abstract class StageHandlerRegistryBase : IStageHandlerRegistry
{
    private readonly Dictionary<PipelineStage, IStageHandler> _byStage;

    /// <summary>Oluşturur.</summary>
    /// <param name="handlers">İşleyiciler.</param>
    protected StageHandlerRegistryBase(IEnumerable<IStageHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        _byStage = [];
        foreach (var handler in handlers)
        {
            _byStage[handler.Stage] = handler;
        }

        All = PipelineStages.Ordered.Where(_byStage.ContainsKey).Select(s => _byStage[s]).ToList();
    }

    /// <summary>Tüm işleyiciler (hat sırasıyla).</summary>
    public IReadOnlyList<IStageHandler> All { get; }

    /// <summary>Aşamanın işleyicisi.</summary>
    /// <param name="stage">Aşama.</param>
    /// <returns>İşleyici ya da <c>null</c>.</returns>
    public IStageHandler? Find(PipelineStage stage) => _byStage.GetValueOrDefault(stage);
}
