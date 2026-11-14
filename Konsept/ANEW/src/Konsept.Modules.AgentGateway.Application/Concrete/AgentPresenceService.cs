using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Application.Base;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;

namespace Konsept.Modules.AgentGateway.Application.Concrete;

/// <summary>
/// Ajan bağlanma/kopma olaylarını Monitoring'in genel olay kaydına yazar (spec 0001 AC-8).
/// </summary>
public sealed class AgentPresenceService : AgentPresenceServiceBase
{
    /// <summary>Bağlanma olayının türü.</summary>
    public const string ConnectedEventType = "agent.connected";

    /// <summary>Kopma olayının türü.</summary>
    public const string DisconnectedEventType = "agent.disconnected";

    private readonly ISystemEventWriter _events;

    /// <summary>Initializes a new instance of the <see cref="AgentPresenceService"/> class.</summary>
    /// <param name="registry">Ajan oturum kaydı.</param>
    /// <param name="events">Genel olay kaydı.</param>
    /// <param name="timeProvider">Saat kaynağı.</param>
    public AgentPresenceService(IAgentConnectionRegistry registry, ISystemEventWriter events, TimeProvider timeProvider)
        : base(registry, timeProvider)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    /// <inheritdoc />
    protected override Task OnConnectedAsync(AgentSession session, CancellationToken cancellationToken) =>
        _events.WriteAsync(
            new SystemEventEntry(ConnectedEventType, SystemEventLevel.Info, $"Ajan bağlandı: {session.MachineName} (v{session.AgentVersion})"),
            cancellationToken);

    /// <inheritdoc />
    protected override Task OnDisconnectedAsync(AgentSession session, CancellationToken cancellationToken) =>
        _events.WriteAsync(
            new SystemEventEntry(DisconnectedEventType, SystemEventLevel.Warning, $"Ajan bağlantısı koptu: {session.MachineName}"),
            cancellationToken);
}
