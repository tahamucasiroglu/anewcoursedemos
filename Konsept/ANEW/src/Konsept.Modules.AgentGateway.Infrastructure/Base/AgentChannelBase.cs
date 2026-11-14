using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Infrastructure.Base;

/// <summary>
/// Ajan komut kanallarının temeli: bağlantı kimliğini doğrular, gönderimi alt sınıfa bırakır.
/// </summary>
public abstract class AgentChannelBase : IAgentChannel
{
    /// <inheritdoc />
    public Task<AgentPingReply> PingAsync(string connectionId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        return InvokePingAsync(connectionId, cancellationToken);
    }

    /// <summary>Komutu gönderir ve cevabı bekler.</summary>
    /// <param name="connectionId">Ajan bağlantısı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ajanın cevabı.</returns>
    protected abstract Task<AgentPingReply> InvokePingAsync(string connectionId, CancellationToken cancellationToken);
}
