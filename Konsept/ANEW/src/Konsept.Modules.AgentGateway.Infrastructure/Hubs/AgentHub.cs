using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace Konsept.Modules.AgentGateway.Infrastructure.Hubs;

/// <summary>
/// Ajanın bağlandığı SignalR hub'ı. Bağlantıyı ajan açar (NAT arkasında); her yeniden bağlanmada kendini
/// yeniden kaydeder. Bu spec'te kimliksizdir (yalnızca yerel ağ); 0002/0012 kimlik ekler.
/// </summary>
public sealed class AgentHub : Hub
{
    private readonly IAgentPresenceService _presence;

    /// <summary>Initializes a new instance of the <see cref="AgentHub"/> class.</summary>
    /// <param name="presence">Ajan varlık servisi.</param>
    public AgentHub(IAgentPresenceService presence)
    {
        _presence = presence ?? throw new ArgumentNullException(nameof(presence));
    }

    /// <summary>Ajan kendini kaydeder.</summary>
    /// <param name="registration">Ajanın bilgileri.</param>
    /// <returns>Görev.</returns>
    public Task Register(AgentRegistration registration) =>
        _presence.RegisterAsync(Context.ConnectionId, registration, Context.ConnectionAborted);

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _presence.DisconnectAsync(Context.ConnectionId, CancellationToken.None).ConfigureAwait(false);
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}
