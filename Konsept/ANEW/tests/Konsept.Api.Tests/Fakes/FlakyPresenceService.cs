using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;
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
    public AgentStatusDto GetStatus(Guid? userId) => inner.GetStatus(userId);

    /// <inheritdoc />
    public Task<AgentRegisterReply> RegisterAsync(string connectionId, AgentOwner owner, AgentRegistration registration, CancellationToken cancellationToken) =>
        state.ShouldFail(connectionId)
            ? Task.FromException<AgentRegisterReply>(new HubException("kayıt geçici olarak reddedildi"))
            : inner.RegisterAsync(connectionId, owner, registration, cancellationToken);

    /// <inheritdoc />
    public Task<bool> BeginUpdateAsync(string connectionId, AgentOwner owner, CancellationToken cancellationToken) =>
        inner.BeginUpdateAsync(connectionId, owner, cancellationToken); // Test kurulumu (plan 0023 A-1): arayüze uyum.

    /// <inheritdoc />
    public void EndUpdate(string connectionId, AgentOwner owner) => inner.EndUpdate(connectionId, owner);

    /// <inheritdoc />
    public bool ReportComponents(string connectionId, AgentOwner owner, IReadOnlyList<string> components) =>
        inner.ReportComponents(connectionId, owner, components);

    /// <inheritdoc />
    public Task DisconnectAsync(string connectionId, CancellationToken cancellationToken) =>
        inner.DisconnectAsync(connectionId, cancellationToken);
}
