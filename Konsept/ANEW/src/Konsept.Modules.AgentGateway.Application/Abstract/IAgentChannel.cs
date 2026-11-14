using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Application.Abstract;

/// <summary>
/// API → ajan komut kanalı (uygulaması SignalR ile Infrastructure'da).
/// </summary>
public interface IAgentChannel
{
    /// <summary>Ajana "test et" komutunu gönderir ve cevabını bekler.</summary>
    /// <param name="connectionId">Ajanın bağlantı kimliği.</param>
    /// <param name="cancellationToken">İptal (zaman aşımı dahil).</param>
    /// <returns>Ajanın cevabı.</returns>
    Task<AgentPingReply> PingAsync(string connectionId, CancellationToken cancellationToken);
}
