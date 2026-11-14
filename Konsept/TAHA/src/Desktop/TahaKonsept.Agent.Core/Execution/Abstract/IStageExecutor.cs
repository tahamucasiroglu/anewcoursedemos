using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Agent.Core.Execution.Abstract;

/// <summary>
/// Tek bir pipeline aşamasını ajanda yürütür (Images, Speech, Align, SceneRender, Compose, Mux). Girdi
/// <c>StageCommand.PayloadJson</c>'dan okunur (Shared.Contracts <c>Agent.Payloads</c>), çıktılar iş klasörüne yazılır.
/// </summary>
public interface IStageExecutor
{
    /// <summary>Yürütülen aşama.</summary>
    PipelineStage Stage { get; }

    /// <summary>Aşamayı yürütür. İptalde <see cref="OperationCanceledException"/> fırlatabilir; yarım çıktı kalıcı olmaz.</summary>
    /// <param name="context">Yürütme bağlamı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Artefaktlar ya da hata.</returns>
    Task<Result<StageExecutionOutput>> ExecuteAsync(StageExecutionContext context, CancellationToken ct);
}
