using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Agent.Core.Execution.Base;

/// <summary>
/// Dağıtıcı tabanı: yürütücüleri aşamaya göre sözlükte tutar (aynı aşamada birden çok kayıt varsa sonuncusu geçerli,
/// böylece uygulama varsayılanı ezebilir).
/// </summary>
public abstract class StageExecutorDispatcherBase : IStageExecutorDispatcher
{
    private readonly Dictionary<PipelineStage, IStageExecutor> _executors;

    /// <summary>Oluşturur.</summary>
    /// <param name="executors">Yürütücüler.</param>
    protected StageExecutorDispatcherBase(IEnumerable<IStageExecutor> executors)
    {
        ArgumentNullException.ThrowIfNull(executors);
        _executors = [];
        foreach (var executor in executors)
        {
            _executors[executor.Stage] = executor;
        }
    }

    /// <inheritdoc />
    public bool CanExecute(PipelineStage stage) => _executors.ContainsKey(stage);

    /// <inheritdoc />
    public Task<Result<StageExecutionOutput>> DispatchAsync(StageExecutionContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _executors.TryGetValue(context.Command.Stage, out var executor)
            ? executor.ExecuteAsync(context, ct)
            : Task.FromResult<Result<StageExecutionOutput>>(
                Error.Validation("agent.stage_not_supported", "Bu aşama ajanda çalıştırılamaz: " + context.Command.Stage));
    }
}
