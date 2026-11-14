using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Agent.Core.Execution.Abstract;

/// <summary>Komutun aşamasına göre doğru <see cref="IStageExecutor"/>'ı seçip çalıştırır.</summary>
public interface IStageExecutorDispatcher
{
    /// <summary>Bu aşama ajanda yürütülebilir mi.</summary>
    /// <param name="stage">Aşama.</param>
    /// <returns>Yürütücü varsa <c>true</c>.</returns>
    bool CanExecute(PipelineStage stage);

    /// <summary>Aşamayı yürütür; yürütücü yoksa <c>agent.stage_not_supported</c> hatası döner.</summary>
    /// <param name="context">Yürütme bağlamı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<StageExecutionOutput>> DispatchAsync(StageExecutionContext context, CancellationToken ct);
}
