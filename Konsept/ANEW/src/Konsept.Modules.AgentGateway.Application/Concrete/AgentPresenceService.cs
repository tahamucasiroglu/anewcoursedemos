using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Application.Base;
using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.AgentGateway.Domain.Rules;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.AgentGateway.Application.Concrete;

/// <summary>
/// Ajan bağlanma/kopma olaylarını Monitoring'in genel olay kaydına yazar (spec 0001 AC-8). Olay yazılamazsa
/// (ör. veritabanı kapalı) hata loglanır; ajanın bağlantı durumu etkilenmez.
/// </summary>
public sealed partial class AgentPresenceService : AgentPresenceServiceBase
{
    /// <summary>Bağlanma olayının türü.</summary>
    public const string ConnectedEventType = "agent.connected";

    /// <summary>Kopma olayının türü.</summary>
    public const string DisconnectedEventType = "agent.disconnected";

    private readonly ISystemEventWriter _events;
    private readonly ILogger<AgentPresenceService> _logger;

    /// <summary>Initializes a new instance of the <see cref="AgentPresenceService"/> class.</summary>
    /// <param name="registry">Ajan oturum kaydı.</param>
    /// <param name="events">Genel olay kaydı.</param>
    /// <param name="timeProvider">Saat kaynağı.</param>
    /// <param name="logger">Log.</param>
    public AgentPresenceService(
        IAgentConnectionRegistry registry,
        ISystemEventWriter events,
        TimeProvider timeProvider,
        ILogger<AgentPresenceService> logger)
        : base(registry, timeProvider)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Initializes a new instance of the <see cref="AgentPresenceService"/> class.</summary>
    /// <param name="registry">Ajan oturum kaydı.</param>
    /// <param name="events">Genel olay kaydı.</param>
    /// <param name="timeProvider">Saat kaynağı.</param>
    /// <param name="logger">Log.</param>
    /// <param name="components">Kurulu bileşen deposu (spec 0006 Req 19).</param>
    public AgentPresenceService(
        IAgentConnectionRegistry registry,
        ISystemEventWriter events,
        TimeProvider timeProvider,
        ILogger<AgentPresenceService> logger,
        IInstalledComponentStore components)
        : this(registry, events, timeProvider, logger)
    {
        Components = components ?? throw new ArgumentNullException(nameof(components));
    }

    /// <summary>Initializes a new instance of the <see cref="AgentPresenceService"/> class (sürüm kapısı ve üretim durumuyla, spec 0023).</summary>
    /// <param name="registry">Ajan oturum kaydı.</param>
    /// <param name="events">Genel olay kaydı yazıcısı.</param>
    /// <param name="timeProvider">Saat kaynağı.</param>
    /// <param name="logger">Teknik log.</param>
    /// <param name="components">Kurulu bileşen deposu.</param>
    /// <param name="gate">Sürüm kapısı.</param>
    /// <param name="activity">Kullanıcının "Üretiliyor" işi var mı.</param>
    public AgentPresenceService(
        IAgentConnectionRegistry registry,
        ISystemEventWriter events,
        TimeProvider timeProvider,
        ILogger<AgentPresenceService> logger,
        IInstalledComponentStore components,
        AgentVersionGate gate,
        IProductionActivityReader activity)
        : this(registry, events, timeProvider, logger, components)
    {
        Gate = gate ?? throw new ArgumentNullException(nameof(gate));
        Activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    /// <inheritdoc />
    protected override Task OnConnectedAsync(AgentSession session, CancellationToken cancellationToken) =>
        _events.WriteAsync(
            new SystemEventEntry(ConnectedEventType, SystemEventLevel.Info, $"Ajan bağlandı: {session.MachineName} (v{session.AgentVersion})", OwnerOf(session)),
            cancellationToken);

    /// <inheritdoc />
    protected override Task OnDisconnectedAsync(AgentSession session, CancellationToken cancellationToken) =>
        _events.WriteAsync(
            new SystemEventEntry(DisconnectedEventType, SystemEventLevel.Warning, $"Ajan bağlantısı koptu: {session.MachineName}", OwnerOf(session)),
            cancellationToken);

    /// <inheritdoc />
    protected override void OnSideEffectFailed(Exception exception) => LogEventWriteFailed(_logger, exception);

    // Ajan olayları sahibine bağlıdır ve yalnızca onun Günlük şeridinde görünür (spec 0012 Req 13).
    private static Guid? OwnerOf(AgentSession session) => session.UserId == Guid.Empty ? null : session.UserId;

    [LoggerMessage(Level = LogLevel.Error, Message = "Ajan olayı olay kaydına yazılamadı; ajan durumu etkilenmedi.")]
    private static partial void LogEventWriteFailed(ILogger logger, Exception exception);
}
