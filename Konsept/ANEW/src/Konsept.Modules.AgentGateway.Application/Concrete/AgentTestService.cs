using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Application.Base;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Application.Concrete;

/// <summary>
/// "Ajanı test et" komutunu ajan kanalı üzerinden gönderir.
/// </summary>
public sealed class AgentTestService : AgentTestServiceBase
{
    private readonly IAgentChannel _channel;

    /// <summary>Initializes a new instance of the <see cref="AgentTestService"/> class.</summary>
    /// <param name="registry">Ajan oturum kaydı.</param>
    /// <param name="channel">Ajan komut kanalı.</param>
    /// <param name="timeProvider">Saat kaynağı.</param>
    public AgentTestService(IAgentConnectionRegistry registry, IAgentChannel channel, TimeProvider timeProvider)
        : base(registry, timeProvider)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
    }

    /// <inheritdoc />
    protected override Task<AgentPingReply> SendPingAsync(string connectionId, CancellationToken cancellationToken) =>
        _channel.PingAsync(connectionId, cancellationToken);

    /// <inheritdoc />
    protected override bool IsConnectionLost(Exception exception) =>
        exception is IOException or InvalidOperationException;
}
