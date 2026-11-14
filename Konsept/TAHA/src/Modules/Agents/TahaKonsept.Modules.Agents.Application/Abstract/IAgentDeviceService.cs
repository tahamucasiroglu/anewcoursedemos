using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Oturumdaki kullanıcının eşlenmiş cihazları: <see cref="ListAsync"/> (<c>GET api/agents</c>, canlı çevrimiçi durumuyla),
/// <see cref="RemoveAsync"/> (<c>DELETE api/agents/{id}</c>: eşlemeyi iptal eder, token'ları iptal ettirir, bağlantıyı keser).
/// </summary>
public interface IAgentDeviceService
{
    /// <summary>Cihazları listeler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cihazlar.</returns>
    Task<Result<IReadOnlyList<AgentDeviceInfo>>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Cihaz eşlemesini kaldırır.</summary>
    /// <param name="id">Cihaz kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
