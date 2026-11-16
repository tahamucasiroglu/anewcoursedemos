using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Bellek konu önerisi deposu (plan 0014); her işlem kullanıcı kimliğiyle süzülür (BR-1).</summary>
public sealed class InMemoryTopicSuggestionRepository : ITopicSuggestionRepository
{
    private readonly Lock _gate = new();
    private readonly List<TopicSuggestion> _rows = [];

    /// <summary>Gets tüm satırlar.</summary>
    public IReadOnlyList<TopicSuggestion> Rows
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
    public Task<IReadOnlyList<TopicSuggestion>> ListAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<TopicSuggestion>>([.. _rows.Where(s => s.UserId == userId && s.ProfileId == profileId).OrderBy(s => s.CreatedAt)]);
        }
    }

    /// <inheritdoc />
    public Task AddRangeAsync(IReadOnlyCollection<TopicSuggestion> suggestions, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _rows.AddRange(suggestions);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RestoreSelectedAsync(Guid userId, Guid profileId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                var s = _rows[i];
                if (s.UserId == userId && s.ProfileId == profileId && s.State == SuggestionState.Selected && ids.Contains(s.Id))
                {
                    _rows[i] = new TopicSuggestion
                    {
                        Id = s.Id,
                        UserId = s.UserId,
                        ProfileId = s.ProfileId,
                        Title = s.Title,
                        TitleKey = s.TitleKey,
                        Summary = s.Summary,
                        State = SuggestionState.Pending,
                        CreatedAt = s.CreatedAt,
                    };
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>Gets or sets a value indicating whether sonraki durum değişikliği hata versin (veritabanı hatası benzetimi; review r1 M-1).</summary>
    public bool FailNextStateChange { get; set; }

    /// <inheritdoc />
    public Task SetPendingStateAsync(Guid userId, Guid profileId, IReadOnlyCollection<Guid>? ids, SuggestionState state, CancellationToken cancellationToken)
    {
        if (FailNextStateChange)
        {
            FailNextStateChange = false;
            throw new InvalidOperationException("Benzetilmiş veritabanı hatası.");
        }

        lock (_gate)
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                var s = _rows[i];
                if (s.UserId == userId && s.ProfileId == profileId && s.State == SuggestionState.Pending && (ids is null || ids.Contains(s.Id)))
                {
                    _rows[i] = new TopicSuggestion
                    {
                        Id = s.Id,
                        UserId = s.UserId,
                        ProfileId = s.ProfileId,
                        Title = s.Title,
                        TitleKey = s.TitleKey,
                        Summary = s.Summary,
                        State = state,
                        CreatedAt = s.CreatedAt,
                    };
                }
            }
        }

        return Task.CompletedTask;
    }
}
