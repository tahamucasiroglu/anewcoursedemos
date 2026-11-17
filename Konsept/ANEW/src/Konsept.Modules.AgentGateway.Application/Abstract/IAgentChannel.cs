using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Application.Abstract;

/// <summary>
/// API → ajan komut kanalı (uygulaması SignalR ile Infrastructure'da). Komutlar bağlantı kimliğine gider; hangi bağlantının kullanılacağına
/// servis karar verir (sahibin geçerli oturumu).
/// </summary>
public interface IAgentChannel
{
    /// <summary>Ajana "test et" komutunu gönderir ve cevabını bekler.</summary>
    /// <param name="connectionId">Ajanın bağlantı kimliği.</param>
    /// <param name="cancellationToken">İptal (zaman aşımı dahil).</param>
    /// <returns>Ajanın cevabı.</returns>
    Task<AgentPingReply> PingAsync(string connectionId, CancellationToken cancellationToken);

    /// <summary>Araç denetimi (spec 0017 Req 14).</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cevap.</returns>
    Task<AgentToolCheckReply> CheckToolsAsync(string connectionId, AgentToolCheckRequest request, CancellationToken cancellationToken);

    /// <summary>Bileşen kurulum komutu (spec 0027).</summary>
    /// <param name="connectionId">Ajan bağlantısı.</param>
    /// <param name="request">Kurulacak bileşenler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cevap.</returns>
    Task<AgentInstallReply> InstallComponentsAsync(string connectionId, AgentInstallRequest request, CancellationToken cancellationToken);

    /// <summary>Yerel aşama (spec 0017 Req 13).</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="command">Komut.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cevap.</returns>
    Task<AgentStageReply> RunStageAsync(string connectionId, AgentStageCommand command, CancellationToken cancellationToken);

    /// <summary>Aşamayı keser (spec 0017 Req 9).</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task CancelStageAsync(string connectionId, Guid jobId, CancellationToken cancellationToken);

    /// <summary>Yerel akış (spec 0017 Req 21).</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cevap.</returns>
    Task<AgentStreamReply> OpenStreamAsync(string connectionId, AgentStreamRequest request, CancellationToken cancellationToken);

    /// <summary>Dosya eylemi (spec 0007 Req 4).</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cevap.</returns>
    Task<AgentFileActionReply> OpenFileAsync(string connectionId, AgentFileActionRequest request, CancellationToken cancellationToken);

    /// <summary>Dosyaları kalıcı sil (spec 0019 Req 4).</summary>
    /// <param name="connectionId">Bağlantı.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cevap.</returns>
    Task<AgentDeleteFilesReply> DeleteFilesAsync(string connectionId, AgentDeleteFilesRequest request, CancellationToken cancellationToken);
}
