using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// <c>AgentHub</c>'ın tüm mantığı (hub ince kalır): bağlantı kaydı/doğrulama, Hello ile cihaz güncelleme, heartbeat,
/// aşama olaylarını <see cref="IAgentStageEventHandler"/>'a, günlükleri <see cref="IAgentLogSink"/>'e, ses önizleme sonucunu
/// <see cref="ISpeechPreviewCorrelator"/>'a iletme.
/// </summary>
public interface IAgentHubService
{
    /// <summary>Yeni bağlantı: cihaz iptal edilmişse ya da başkasına aitse başarısız döner (hub bağlantıyı keser).</summary>
    /// <param name="connection">Bağlantı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> ConnectedAsync(AgentConnection connection, CancellationToken cancellationToken = default);

    /// <summary>Bağlantı koptu.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task DisconnectedAsync(string connectionId, CancellationToken cancellationToken = default);

    /// <summary>Ajan tanıtımı.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="hello">Tanıtım.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç (cihaz bulunamazsa başarısız; hub bağlantıyı keser).</returns>
    Task<Result> HelloAsync(AgentCallContext context, AgentHello hello, CancellationToken cancellationToken = default);

    /// <summary>Canlılık sinyali.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task HeartbeatAsync(AgentCallContext context, CancellationToken cancellationToken = default);

    /// <summary>Aşama ilerlemesi.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StageProgressAsync(AgentCallContext context, StageProgressReport report, CancellationToken cancellationToken = default);

    /// <summary>Aşama tamamlandı.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StageCompletedAsync(AgentCallContext context, StageResultReport report, CancellationToken cancellationToken = default);

    /// <summary>Aşama başarısız.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="report">Rapor.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StageFailedAsync(AgentCallContext context, StageFailureReport report, CancellationToken cancellationToken = default);

    /// <summary>Günlük kaydı.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="entry">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task LogAsync(AgentCallContext context, AgentLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Ses önizleme sonucu.</summary>
    /// <param name="context">Çağrı bağlamı.</param>
    /// <param name="result">Sonuç.</param>
    /// <returns>Görev.</returns>
    Task SpeechPreviewCompletedAsync(AgentCallContext context, SpeechPreviewResult result);
}
