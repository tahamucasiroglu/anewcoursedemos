using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Base;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.Monitoring.Application.Concrete;

/// <summary>
/// Sistem durumunu veritabanı yoklaması, olay kaydı, ajan durumu ve ürün versiyonundan derler.
/// </summary>
public sealed class SystemStatusService : SystemStatusServiceBase
{
    private readonly IDatabaseProbe _probe;
    private readonly ISystemEventReader _events;
    private readonly IAgentStatusReader _agent;
    private readonly IProductVersionProvider _version;

    /// <summary>Initializes a new instance of the <see cref="SystemStatusService"/> class.</summary>
    /// <param name="probe">Veritabanı yoklaması.</param>
    /// <param name="events">Olay kaydı okuyucusu.</param>
    /// <param name="agent">Ajan durumu (AgentGateway Contracts).</param>
    /// <param name="version">Ürün versiyonu.</param>
    public SystemStatusService(IDatabaseProbe probe, ISystemEventReader events, IAgentStatusReader agent, IProductVersionProvider version)
    {
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _version = version ?? throw new ArgumentNullException(nameof(version));
    }

    /// <inheritdoc />
    protected override Task<bool> IsDatabaseAvailableAsync(CancellationToken cancellationToken) =>
        _probe.CanConnectAsync(cancellationToken);

    /// <inheritdoc />
    protected override Task<SystemEventDto?> ReadLastEventAsync(CancellationToken cancellationToken) =>
        _events.GetLatestAsync(cancellationToken);

    /// <inheritdoc />
    protected override string ReadVersion() => _version.Version;

    /// <inheritdoc />
    protected override AgentStatusDto ReadAgentStatus() => _agent.GetStatus();
}
