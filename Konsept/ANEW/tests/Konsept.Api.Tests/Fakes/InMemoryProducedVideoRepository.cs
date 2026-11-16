using System.Collections.Concurrent;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Video kayıtlarının bellek deposu (plan 0017).</summary>
public sealed class InMemoryProducedVideoRepository : IProducedVideoRepository
{
    private readonly ConcurrentDictionary<Guid, ProducedVideo> _rows = new();

    /// <summary>Gets satırlar.</summary>
    public IReadOnlyCollection<ProducedVideo> Rows => [.. _rows.Values];

    /// <inheritdoc />
    public Task AddAsync(ProducedVideo video, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(video);
        _rows[video.Id] = video;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProducedVideo>> ListAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProducedVideo>>([.. _rows.Values.Where(v => v.UserId == userId && v.ProfileId == profileId).OrderByDescending(v => v.EndedAt)]);

    /// <inheritdoc />
    public Task<ProducedVideo?> GetAsync(Guid userId, Guid videoId, CancellationToken cancellationToken) =>
        Task.FromResult(_rows.TryGetValue(videoId, out var v) && v.UserId == userId ? v : null);

    /// <inheritdoc />
    public Task<IReadOnlyList<ProducedVideo>> ListByConceptAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProducedVideo>>([.. _rows.Values.Where(v => v.UserId == userId && v.ConceptId == conceptId).OrderBy(v => v.EndedAt)]);
}
