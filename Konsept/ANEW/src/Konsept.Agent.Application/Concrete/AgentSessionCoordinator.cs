using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Application.Base;

namespace Konsept.Agent.Application.Concrete;

/// <summary>Oturum akışı; bekleme ve saat <see cref="TimeProvider"/>'dan.</summary>
/// <param name="connection">Sunucu bağlantısı.</param>
/// <param name="pairing">Eşleştirme uçları.</param>
/// <param name="tokens">Belirteç deposu.</param>
/// <param name="info">Ajan bilgisi.</param>
/// <param name="options">Zamanlamalar.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
public sealed class AgentSessionCoordinator(
    IServerConnection connection,
    IPairingClient pairing,
    IAgentTokenStore tokens,
    IAgentInfoProvider info,
    AgentSessionOptions options,
    TimeProvider timeProvider) : AgentSessionCoordinatorBase(connection, pairing, tokens, info, options)
{
    /// <inheritdoc />
    protected override Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, timeProvider, cancellationToken);

    /// <inheritdoc />
    protected override DateTimeOffset UtcNow() => timeProvider.GetUtcNow();
}
