using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Base;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Shared.Kernel.Abstract;

namespace Konsept.Modules.Monitoring.Application.Concrete;

/// <summary>
/// Sistem durumunu veritabanı yoklaması, olay kaydı, ajan durumu ve ürün versiyonundan derler. Son olay isteği yapan
/// kullanıcıya göre süzülür (spec 0002 Req 18): kendi olayları + kullanıcısız sistem/ajan olayları.
/// </summary>
public sealed class SystemStatusService : SystemStatusServiceBase
{
    private readonly IDatabaseProbe _probe;
    private readonly ISystemEventRepository _events;
    private readonly IAgentStatusReader _agent;
    private readonly IProductVersionProvider _version;
    private readonly ICurrentUser _currentUser;

    /// <summary>Initializes a new instance of the <see cref="SystemStatusService"/> class.</summary>
    /// <param name="probe">Veritabanı yoklaması.</param>
    /// <param name="events">Olay deposu.</param>
    /// <param name="agent">Ajan durumu (AgentGateway Contracts).</param>
    /// <param name="version">Ürün versiyonu.</param>
    /// <param name="currentUser">İsteği yapan kullanıcı.</param>
    public SystemStatusService(
        IDatabaseProbe probe,
        ISystemEventRepository events,
        IAgentStatusReader agent,
        IProductVersionProvider version,
        ICurrentUser currentUser)
    {
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _version = version ?? throw new ArgumentNullException(nameof(version));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <inheritdoc />
    protected override Task<bool> IsDatabaseAvailableAsync(CancellationToken cancellationToken) =>
        _probe.CanConnectAsync(cancellationToken);

    /// <inheritdoc />
    protected override async Task<SystemEventDto?> ReadLastEventAsync(CancellationToken cancellationToken)
    {
        // Günlük şeridi yalnızca kullanıcının kendi kaydını gösterir (spec 0021 Req 7; 0002'deki kullanıcısız olay istisnası kalktı).
        if (_currentUser.UserId is not { } userId)
        {
            return null;
        }

        var e = await _events.GetLatestOwnedAsync(userId, cancellationToken).ConfigureAwait(false);
        return e is null ? null : SystemEventServiceBase.ToDto(e);
    }

    /// <inheritdoc />
    protected override string ReadVersion() => _version.Version;

    /// <inheritdoc />
    protected override AgentStatusDto ReadAgentStatus() => _agent.GetStatus(_currentUser.UserId);
}
