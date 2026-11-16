using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Concepts.Infrastructure.Persistence.Concrete;

/// <summary>Konu önerilerinin EF Core + PostgreSQL uygulaması; her sorgu <c>user_id</c> ile süzülür (BR-1).</summary>
public sealed class TopicSuggestionRepository : EfRepositoryBase<ConceptsDbContext>, ITopicSuggestionRepository
{
    /// <summary>Initializes a new instance of the <see cref="TopicSuggestionRepository"/> class.</summary>
    /// <param name="context">Bağlam.</param>
    public TopicSuggestionRepository(ConceptsDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TopicSuggestion>> ListAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        await Context.TopicSuggestions.AsNoTracking()
            .Where(s => s.UserId == userId && s.ProfileId == profileId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task AddRangeAsync(IReadOnlyCollection<TopicSuggestion> suggestions, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(suggestions);
        if (suggestions.Count == 0)
        {
            return;
        }

        Context.TopicSuggestions.AddRange(suggestions);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Context.ChangeTracker.Clear();
    }

    /// <inheritdoc />
    public Task SetPendingStateAsync(Guid userId, Guid profileId, IReadOnlyCollection<Guid>? ids, SuggestionState state, CancellationToken cancellationToken)
    {
        var query = Context.TopicSuggestions.Where(s => s.UserId == userId && s.ProfileId == profileId && s.State == SuggestionState.Pending);
        if (ids is not null)
        {
            query = query.Where(s => ids.Contains(s.Id));
        }

        return query.ExecuteUpdateAsync(s => s.SetProperty(x => x.State, state), cancellationToken);
    }

    /// <inheritdoc />
    public Task RestoreSelectedAsync(Guid userId, Guid profileId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Context.TopicSuggestions
            .Where(s => s.UserId == userId && s.ProfileId == profileId && s.State == SuggestionState.Selected && ids.Contains(s.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.State, SuggestionState.Pending), cancellationToken);
}
