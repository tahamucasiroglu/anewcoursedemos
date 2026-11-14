using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;

namespace Konsept.Modules.AgentGateway.Application.Base;

/// <summary>
/// Ajan varlığının ortak mantığı: oturum kaydı ve durum okuma; olay yazımını alt sınıfa bırakır.
/// </summary>
public abstract class AgentPresenceServiceBase : IAgentPresenceService
{
    /// <summary>Initializes a new instance of the <see cref="AgentPresenceServiceBase"/> class.</summary>
    /// <param name="registry">Ajan oturum kaydı.</param>
    /// <param name="timeProvider">Saat kaynağı.</param>
    protected AgentPresenceServiceBase(IAgentConnectionRegistry registry, TimeProvider timeProvider)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Gets ajan oturum kaydı.</summary>
    protected IAgentConnectionRegistry Registry { get; }

    /// <summary>Gets saat kaynağı.</summary>
    protected TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public AgentStatusDto GetStatus()
    {
        var current = Registry.Current;
        return current is null
            ? new AgentStatusDto(false, null, null, null)
            : new AgentStatusDto(true, current.MachineName, current.AgentVersion, current.ConnectedAt);
    }

    /// <inheritdoc />
    public async Task RegisterAsync(string connectionId, AgentRegistration registration, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentNullException.ThrowIfNull(registration);
        var session = new AgentSession(connectionId, registration.MachineName, registration.AgentVersion, TimeProvider.GetUtcNow());
        Registry.Set(session);
        await OnConnectedAsync(session, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(string connectionId, CancellationToken cancellationToken)
    {
        var removed = Registry.Remove(connectionId);
        if (removed is not null)
        {
            await OnDisconnectedAsync(removed, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Ajan bağlandığında çağrılır.</summary>
    /// <param name="session">Yeni oturum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task OnConnectedAsync(AgentSession session, CancellationToken cancellationToken);

    /// <summary>Geçerli ajanın bağlantısı koptuğunda çağrılır.</summary>
    /// <param name="session">Kapanan oturum.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task OnDisconnectedAsync(AgentSession session, CancellationToken cancellationToken);
}
