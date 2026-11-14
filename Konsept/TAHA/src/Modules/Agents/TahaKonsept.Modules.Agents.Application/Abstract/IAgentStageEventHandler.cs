using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Ajandan gelen aşama olaylarının alıcısı (uzatma noktası). Varsayılan kayıt hiçbir şey yapmaz (<c>NullAgentStageEventHandler</c>, TryAdd);
/// Production modülü kendi gerçek uygulamasını kaydeder (Agents modülünden önce ya da <c>Replace</c> ile). Scoped çözülür.
/// </summary>
public interface IAgentStageEventHandler
{
    /// <summary>Aşama ilerlemesi.</summary>
    /// <param name="agent">Ajan bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task OnStageProgressAsync(AgentCallContext agent, StageProgressReport report, CancellationToken cancellationToken = default);

    /// <summary>Aşama tamamlandı.</summary>
    /// <param name="agent">Ajan bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task OnStageCompletedAsync(AgentCallContext agent, StageResultReport report, CancellationToken cancellationToken = default);

    /// <summary>Aşama başarısız.</summary>
    /// <param name="agent">Ajan bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task OnStageFailedAsync(AgentCallContext agent, StageFailureReport report, CancellationToken cancellationToken = default);
}
