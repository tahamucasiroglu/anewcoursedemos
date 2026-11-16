using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Bellek konsept deposu (plan 0014); gerçek depo gibi her işlemi kullanıcı kimliğiyle süzer (BR-1).</summary>
public sealed class InMemoryConceptRepository : IConceptRepository
{
    private readonly Lock _gate = new();
    private readonly List<Concept> _rows = [];

    /// <summary>Gets tüm satırlar ("veritabanı").</summary>
    public IReadOnlyList<Concept> Rows
    {
        get
        {
            lock (_gate)
            {
                return [.. _rows];
            }
        }
    }

    /// <inheritdoc />
    public Task AddAsync(Concept concept, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _rows.Add(concept);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ConceptPage> ListAsync(Guid userId, Guid profileId, IReadOnlyCollection<ConceptStatus> statuses, int skip, int take, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var query = _rows.Where(c => c.UserId == userId && c.ProfileId == profileId && c.DeletedAt is null && statuses.Contains(c.Status)).ToList();
            var items = query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id).Skip(skip).Take(take).ToList();
            return Task.FromResult(new ConceptPage(items, query.Count));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Concept>> GetManyAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<Concept>>([.. _rows.Where(c => c.UserId == userId && c.DeletedAt is null && ids.Contains(c.Id))]);
        }
    }

    /// <inheritdoc />
    public Task<int> ChangeStatusAsync(Guid userId, IReadOnlyCollection<Guid> ids, ConceptStatus from, ConceptStatus to, DateTimeOffset at, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var changed = 0;
            for (var i = 0; i < _rows.Count; i++)
            {
                var c = _rows[i];
                if (c.UserId == userId && c.DeletedAt is null && c.Status == from && ids.Contains(c.Id))
                {
                    _rows[i] = Copy(c, to, at);
                    changed++;
                }
            }

            return Task.FromResult(changed);
        }
    }

    /// <inheritdoc />
    public Task<bool> UpdateScenesAsync(
        Guid userId,
        Guid conceptId,
        IReadOnlyCollection<ConceptStatus> allowed,
        IReadOnlyList<ConceptScene> scenes,
        bool wordRepetition,
        bool durationMismatch,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var i = _rows.FindIndex(c => c.Id == conceptId && c.UserId == userId && c.DeletedAt is null && allowed.Contains(c.Status));
            if (i < 0)
            {
                return Task.FromResult(false);
            }

            var c = _rows[i];
            _rows[i] = new Concept
            {
                Id = c.Id,
                UserId = c.UserId,
                ProfileId = c.ProfileId,
                Title = c.Title,
                TitleKey = c.TitleKey,
                Source = c.Source,
                Status = c.Status,
                Language = c.Language,
                Format = c.Format,
                TargetDurationSeconds = c.TargetDurationSeconds,
                Scenes = [.. scenes],
                WordRepetitionWarning = wordRepetition,
                DurationMismatchWarning = durationMismatch,
                CreatedAt = c.CreatedAt,
                UpdatedAt = at,
                DeletedAt = c.DeletedAt,
            };
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<HistoryTitle>> ListTitlesAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<HistoryTitle>>(
                [.. _rows.Where(c => c.UserId == userId && c.ProfileId == profileId)
                    .Select(c => new HistoryTitle(c.Title, c.Source == ConceptSource.Topic && c.DeletedAt is null, c.DeletedAt ?? c.CreatedAt))]);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlySet<Guid>> ProfileIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlySet<Guid>>(_rows.Where(c => c.UserId == userId).Select(c => c.ProfileId).ToHashSet());
        }
    }

    /// <inheritdoc />
    public Task<bool> SoftDeleteAsync(Guid userId, Guid conceptId, IReadOnlyCollection<ConceptStatus> allowed, DateTimeOffset at, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var i = _rows.FindIndex(c => c.Id == conceptId && c.UserId == userId && c.DeletedAt is null && allowed.Contains(c.Status));
            if (i < 0)
            {
                return Task.FromResult(false);
            }

            _rows[i] = Copy(_rows[i], _rows[i].Status, at, at);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<ConceptPage> ListDeletedAsync(Guid userId, Guid profileId, int skip, int take, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var query = _rows.Where(c => c.UserId == userId && c.ProfileId == profileId && c.DeletedAt is not null).ToList();
            var items = query.OrderByDescending(c => c.DeletedAt).ThenByDescending(c => c.Id).Skip(skip).Take(take).ToList();
            return Task.FromResult(new ConceptPage(items, query.Count));
        }
    }

    /// <inheritdoc />
    public Task<Concept?> GetDeletedAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_rows.FirstOrDefault(c => c.Id == conceptId && c.UserId == userId && c.DeletedAt is not null));
        }
    }

    /// <inheritdoc />
    public Task<bool> PurgeAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_rows.RemoveAll(c => c.Id == conceptId && c.UserId == userId && c.DeletedAt is not null) > 0);
        }
    }

    private static Concept Copy(Concept c, ConceptStatus status, DateTimeOffset at, DateTimeOffset? deletedAt = null) => new()
    {
        Id = c.Id,
        UserId = c.UserId,
        ProfileId = c.ProfileId,
        Title = c.Title,
        TitleKey = c.TitleKey,
        Source = c.Source,
        Status = status,
        Language = c.Language,
        Format = c.Format,
        TargetDurationSeconds = c.TargetDurationSeconds,
        Scenes = c.Scenes,
        WordRepetitionWarning = c.WordRepetitionWarning,
        DurationMismatchWarning = c.DurationMismatchWarning,
        CreatedAt = c.CreatedAt,
        UpdatedAt = at,
        DeletedAt = deletedAt ?? c.DeletedAt,
    };
}
