using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// İlk birkaç kayıt isteğini sunucu hatasıyla reddeden, sonra gerçek servise devreden varlık servisi.
/// İstemcinin bağlıyken hub'ı yeniden başlatmadan yalnızca kaydı yeniden denediğini kanıtlamak için.
/// </summary>
/// <param name="inner">Gerçek varlık servisi.</param>
/// <param name="state">Paylaşılan sayaç.</param>
public sealed class FlakyPresenceService(IAgentPresenceService inner, FlakyRegistrationState state) : IAgentPresenceService
{
    /// <inheritdoc />
    public AgentStatusDto GetStatus() => inner.GetStatus();

    /// <inheritdoc />
    public Task RegisterAsync(string connectionId, AgentRegistration registration, CancellationToken cancellationToken) =>
        state.ShouldFail(connectionId)
            ? Task.FromException(new HubException("kayıt geçici olarak reddedildi"))
            : inner.RegisterAsync(connectionId, registration, cancellationToken);

    /// <inheritdoc />
    public Task DisconnectAsync(string connectionId, CancellationToken cancellationToken) =>
        inner.DisconnectAsync(connectionId, cancellationToken);
}
