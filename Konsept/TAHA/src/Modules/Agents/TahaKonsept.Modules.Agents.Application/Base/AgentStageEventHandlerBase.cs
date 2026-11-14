using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Base;

/// <summary>
/// Aşama olay alıcısı tabanı: tüm metotlar varsayılan olarak hiçbir şey yapmaz; türetilen sınıf ilgilendiklerini ezer.
/// </summary>
public abstract class AgentStageEventHandlerBase : IAgentStageEventHandler
{
    /// <summary>Aşama ilerlemesi (varsayılan: yok say).</summary>
    /// <param name="agent">Ajan bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public virtual Task OnStageProgressAsync(AgentCallContext agent, StageProgressReport report, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <summary>Aşama tamamlandı (varsayılan: yok say).</summary>
    /// <param name="agent">Ajan bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public virtual Task OnStageCompletedAsync(AgentCallContext agent, StageResultReport report, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <summary>Aşama başarısız (varsayılan: yok say).</summary>
    /// <param name="agent">Ajan bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public virtual Task OnStageFailedAsync(AgentCallContext agent, StageFailureReport report, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
