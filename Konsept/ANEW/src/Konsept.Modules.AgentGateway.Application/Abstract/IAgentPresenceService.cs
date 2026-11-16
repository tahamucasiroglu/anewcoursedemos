using Konsept.Modules.AgentGateway.Contracts.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;

namespace Konsept.Modules.AgentGateway.Application.Abstract;

/// <summary>
/// Ajanın bağlanma/kopma olaylarını işler: oturumu sahibiyle kaydeder ve olayı sahibine bağlı olarak genel olay kaydına yazar
/// (spec 0012 Req 13).
/// </summary>
public interface IAgentPresenceService : IAgentStatusReader
{
    /// <summary>Ajanın kaydını işler.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <param name="owner">Hub kimliğinden gelen sahip ve cihaz.</param>
    /// <param name="registration">Ajanın bilgileri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task RegisterAsync(string connectionId, AgentOwner owner, AgentRegistration registration, CancellationToken cancellationToken);

    /// <summary>Bağlantının kopmasını işler.</summary>
    /// <param name="connectionId">Kopan bağlantının kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task DisconnectAsync(string connectionId, CancellationToken cancellationToken);
}
