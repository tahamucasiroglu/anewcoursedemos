using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.Agent.Core.Execution.Base;

namespace TahaKonsept.Agent.Core.Execution.Concrete;

/// <summary>DI'daki tüm <see cref="IStageExecutor"/> kayıtlarıyla çalışan aşama dağıtıcısı.</summary>
public sealed class StageExecutorDispatcher : StageExecutorDispatcherBase
{
    /// <summary>Oluşturur.</summary>
    /// <param name="executors">Yürütücüler.</param>
    public StageExecutorDispatcher(IEnumerable<IStageExecutor> executors)
        : base(executors)
    {
    }
}
