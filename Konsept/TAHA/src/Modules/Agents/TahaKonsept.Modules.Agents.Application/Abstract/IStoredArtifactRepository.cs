using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Agents.Domain.Entities;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Geçici artefakt kaydı deposu. Önemli metot: <see cref="ListByJobAsync"/> (iş bitince toplu silme).
/// </summary>
public interface IStoredArtifactRepository : IRepository<StoredArtifact>
{
    /// <summary>Kullanıcının bir işe ait artefaktları (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    Task<IReadOnlyList<StoredArtifact>> ListByJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default);
}
