using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Infrastructure.Base;
using Konsept.Modules.AgentGateway.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Konsept.Modules.AgentGateway.Infrastructure.Concrete;

/// <summary>
/// Komutu SignalR "client result" çağrısıyla ajana gönderir ve cevabını bekler.
/// </summary>
public sealed class SignalRAgentChannel : AgentChannelBase
{
    private readonly IHubContext<AgentHub> _hub;

    /// <summary>Initializes a new instance of the <see cref="SignalRAgentChannel"/> class.</summary>
    /// <param name="hub">Ajan hub bağlamı.</param>
    public SignalRAgentChannel(IHubContext<AgentHub> hub)
    {
        _hub = hub ?? throw new ArgumentNullException(nameof(hub));
    }

    /// <inheritdoc />
    protected override Task<AgentPingReply> InvokePingAsync(string connectionId, CancellationToken cancellationToken) =>
        _hub.Clients.Client(connectionId).InvokeAsync<AgentPingReply>(AgentHubContract.Ping, cancellationToken);

    /// <inheritdoc />
    protected override Task<T> InvokeAsync<T>(string connectionId, string method, object argument, CancellationToken cancellationToken) =>
        _hub.Clients.Client(connectionId).InvokeAsync<T>(method, argument, cancellationToken);

    /// <inheritdoc />
    protected override Task SendAsync(string connectionId, string method, object argument, CancellationToken cancellationToken) =>
        _hub.Clients.Client(connectionId).SendAsync(method, argument, cancellationToken);
}
