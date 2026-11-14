using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Application.Abstract;

/// <summary>
/// Ajanın bağlanma/kopma olaylarını işler: oturumu kaydeder ve genel olay kaydına yazar.
/// </summary>
public interface IAgentPresenceService : IAgentStatusReader
{
    /// <summary>Ajanın kaydını işler.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <param name="registration">Ajanın bilgileri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task RegisterAsync(string connectionId, AgentRegistration registration, CancellationToken cancellationToken);

    /// <summary>Bağlantının kopmasını işler.</summary>
    /// <param name="connectionId">Kopan bağlantının kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task DisconnectAsync(string connectionId, CancellationToken cancellationToken);
}
