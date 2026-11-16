using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts;
using Konsept.Modules.AgentGateway.Infrastructure.Base;
using Konsept.Modules.AgentGateway.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Konsept.Modules.AgentGateway.Infrastructure.Concrete;

/// <summary>"Oturum kapandı" bildirimini SignalR ile gönderir (<see cref="AgentHubContract.SessionClosed"/>).</summary>
/// <param name="tracker">Açık bağlantılar.</param>
/// <param name="hub">Ajan hub bağlamı.</param>
public sealed class AgentSessionTerminator(IAgentConnectionTracker tracker, IHubContext<AgentHub> hub) : AgentSessionTerminatorBase(tracker)
{
    /// <inheritdoc />
    protected override async Task NotifyClosedAsync(string connectionId, CancellationToken cancellationToken)
    {
        try
        {
            await hub.Clients.Client(connectionId).SendAsync(AgentHubContract.SessionClosed, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            // Bağlantı zaten kopmuş olabilir; kesme ve 401 yine geçerli.
        }
    }
}
