using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Concepts.Contracts.Dtos;
using Konsept.Modules.Production.Application.Abstract;

namespace Konsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IDislikedVideoTitles"/> (spec 0019 Req 6): profilin Silinenler'deki videoları, video başına en son sürümün başlığı ve silinme
/// anı. Kalıcı silinen video kayıtla birlikte gittiği için listede yoktur.
/// </summary>
/// <param name="videos">Sürüm kayıtları.</param>
public abstract class DislikedVideoTitlesBase(IProducedVideoRepository videos) : IDislikedVideoTitles
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<DislikedVideoTitle>> ListAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        var rows = await videos.ListDeletedAsync(userId, profileId, cancellationToken).ConfigureAwait(false);
        return [.. rows
            .GroupBy(v => v.ConceptId)
            .Select(g => g.MaxBy(v => v.EndedAt)!)
            .Select(v => new DislikedVideoTitle(v.Title, v.DeletedAt ?? v.EndedAt))];
    }
}
