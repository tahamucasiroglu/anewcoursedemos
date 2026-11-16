using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Concepts.Infrastructure.Persistence.Concrete;

/// <summary>Konseptlerin EF Core + PostgreSQL uygulaması; her sorgu <c>user_id</c> ile süzülür (BR-1).</summary>
public sealed class ConceptRepository : EfRepositoryBase<ConceptsDbContext>, IConceptRepository
{
    /// <summary>Initializes a new instance of the <see cref="ConceptRepository"/> class.</summary>
    /// <param name="context">Bağlam.</param>
    public ConceptRepository(ConceptsDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task AddAsync(Concept concept, CancellationToken cancellationToken)
    {
        Context.Concepts.Add(concept);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Context.Entry(concept).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public async Task<ConceptPage> ListAsync(
        Guid userId, Guid profileId, IReadOnlyCollection<ConceptStatus> statuses, int skip, int take, CancellationToken cancellationToken)
    {
        var query = Context.Concepts.AsNoTracking()
            .Where(c => c.UserId == userId && c.ProfileId == profileId && c.DeletedAt == null && statuses.Contains(c.Status));
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id).Skip(skip).Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new ConceptPage(items, total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Concept>> GetManyAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await Context.Concepts.AsNoTracking()
            .Where(c => c.UserId == userId && c.DeletedAt == null && ids.Contains(c.Id))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<int> ChangeStatusAsync(
        Guid userId, IReadOnlyCollection<Guid> ids, ConceptStatus from, ConceptStatus to, DateTimeOffset at, CancellationToken cancellationToken) =>
        Context.Concepts
            .Where(c => c.UserId == userId && c.DeletedAt == null && c.Status == from && ids.Contains(c.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, to).SetProperty(c => c.UpdatedAt, at), cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<HistoryTitle>> ListTitlesAsync(Guid userId, Guid profileId, CancellationToken cancellationToken)
    {
        var rows = await Context.Concepts.AsNoTracking()
            .Where(c => c.UserId == userId && c.ProfileId == profileId)
            .Select(c => new { c.Title, c.Source, c.CreatedAt })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new HistoryTitle(r.Title, r.Source == ConceptSource.Topic, r.CreatedAt))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> ProfileIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var ids = await Context.Concepts.AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => c.ProfileId)
            .Distinct()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return ids.ToHashSet();
    }
}
