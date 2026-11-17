using System.Collections.Concurrent;
using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Video kayıtlarının bellek deposu (plan 0017; silme işareti, kalıcı silme ve klip izi — plan 0019).</summary>
public sealed class InMemoryProducedVideoRepository : IProducedVideoRepository
{
    private readonly ConcurrentDictionary<Guid, ProducedVideo> _rows = new();
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _tombstones = new();

    /// <summary>Gets satırlar.</summary>
    public IReadOnlyCollection<ProducedVideo> Rows => [.. _rows.Values];

    /// <summary>Gets klip izleri (konsept → ilk tamamlanma).</summary>
    public IReadOnlyDictionary<Guid, DateTimeOffset> Tombstones => _tombstones;

    /// <inheritdoc />
    public Task AddAsync(ProducedVideo video, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(video);
        _rows[video.Id] = video;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProducedVideo>> ListAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProducedVideo>>([.. _rows.Values.Where(v => v.UserId == userId && v.ProfileId == profileId && v.DeletedAt is null).OrderByDescending(v => v.EndedAt)]);

    /// <inheritdoc />
    public Task<ProducedVideo?> GetAsync(Guid userId, Guid videoId, CancellationToken cancellationToken) =>
        Task.FromResult(_rows.TryGetValue(videoId, out var v) && v.UserId == userId && v.DeletedAt is null ? v : null);

    /// <inheritdoc />
    public Task<IReadOnlyList<ProducedVideo>> ListByConceptAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProducedVideo>>([.. _rows.Values.Where(v => v.UserId == userId && v.ConceptId == conceptId && v.DeletedAt is null).OrderBy(v => v.EndedAt)]);

    /// <inheritdoc />
    public Task<int> SoftDeleteAsync(Guid userId, Guid conceptId, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var rows = _rows.Values.Where(v => v.UserId == userId && v.ConceptId == conceptId && v.DeletedAt is null).ToList();
        foreach (var row in rows)
        {
            _rows[row.Id] = row with { DeletedAt = at };
        }

        return Task.FromResult(rows.Count);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProducedVideo>> ListDeletedAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProducedVideo>>([.. _rows.Values.Where(v => v.UserId == userId && v.ProfileId == profileId && v.DeletedAt is not null).OrderByDescending(v => v.DeletedAt)]);

    /// <inheritdoc />
    public Task<IReadOnlyList<ProducedVideo>> ListDeletedByConceptAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ProducedVideo>>([.. _rows.Values.Where(v => v.UserId == userId && v.ConceptId == conceptId && v.DeletedAt is not null).OrderBy(v => v.EndedAt)]);

    /// <inheritdoc />
    public Task<bool> PurgeAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken)
    {
        var rows = _rows.Values.Where(v => v.UserId == userId && v.ConceptId == conceptId && v.DeletedAt is not null).ToList();
        if (rows.Count == 0)
        {
            return Task.FromResult(false);
        }

        var first = rows.Min(v => v.EndedAt);
        _tombstones.AddOrUpdate(conceptId, first, (_, old) => old < first ? old : first);
        foreach (var row in rows)
        {
            _rows.TryRemove(row.Id, out _);
        }

        return Task.FromResult(true);
    }
}
