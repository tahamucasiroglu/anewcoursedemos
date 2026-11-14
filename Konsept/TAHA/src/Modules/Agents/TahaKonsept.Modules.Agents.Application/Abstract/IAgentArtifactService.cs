using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Models;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Oturumdaki ajanın artefakt uçları: <see cref="DownloadAsync"/> (<c>GET api/agents/artifacts/{id}</c>),
/// <see cref="AcknowledgeAsync"/> (<c>DELETE api/agents/artifacts/{id}</c>, indirildi → sil).
/// </summary>
public interface IAgentArtifactService
{
    /// <summary>Artefaktı indirmek için açar.</summary>
    /// <param name="id">Artefakt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İçerik.</returns>
    Task<Result<ArtifactContent>> DownloadAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>İndirmeyi onaylar; dosya silinir.</summary>
    /// <param name="id">Artefakt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> AcknowledgeAsync(Guid id, CancellationToken cancellationToken = default);
}
