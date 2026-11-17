using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>
/// Video sürüm kayıtları deposu (spec 0017 Req 22): ekleme, okuma, silme işareti ve kalıcı silme (spec 0019; içerik değişmez); her işlem
/// sahibine göre süzülür (BR-1).
/// </summary>
public sealed class ProducedVideoRepository : EfRepositoryBase<ProductionDbContext>, IProducedVideoRepository
{
    /// <summary>Initializes a new instance of the <see cref="ProducedVideoRepository"/> class.</summary>
    /// <param name="context">Bağlam.</param>
    public ProducedVideoRepository(ProductionDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task AddAsync(ProducedVideo video, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(video);
        Context.Videos.Add(video);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Context.Entry(video).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProducedVideo>> ListAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        await Context.Videos.AsNoTracking()
            .Where(v => v.UserId == userId && v.ProfileId == profileId && v.DeletedAt == null)
            .OrderByDescending(v => v.EndedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<ProducedVideo?> GetAsync(Guid userId, Guid videoId, CancellationToken cancellationToken) =>
        Context.Videos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == videoId && v.UserId == userId && v.DeletedAt == null, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProducedVideo>> ListByConceptAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken) =>
        await Context.Videos.AsNoTracking()
            .Where(v => v.UserId == userId && v.ConceptId == conceptId && v.DeletedAt == null)
            .OrderBy(v => v.EndedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<int> SoftDeleteAsync(Guid userId, Guid conceptId, DateTimeOffset at, CancellationToken cancellationToken) =>
        Context.Videos
            .Where(v => v.UserId == userId && v.ConceptId == conceptId && v.DeletedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.DeletedAt, at), cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProducedVideo>> ListDeletedAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        await Context.Videos.AsNoTracking()
            .Where(v => v.UserId == userId && v.ProfileId == profileId && v.DeletedAt != null)
            .OrderByDescending(v => v.DeletedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProducedVideo>> ListDeletedByConceptAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken) =>
        await Context.Videos.AsNoTracking()
            .Where(v => v.UserId == userId && v.ConceptId == conceptId && v.DeletedAt != null)
            .OrderBy(v => v.EndedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> PurgeAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var rows = Context.Videos.Where(v => v.UserId == userId && v.ConceptId == conceptId && v.DeletedAt != null);
        var first = await rows.Select(v => (DateTimeOffset?)v.EndedAt).MinAsync(cancellationToken).ConfigureAwait(false);
        if (first is null)
        {
            return false;
        }

        // Klip izi (B-130): aynı konsept için önceden iz varsa en eski an kalır.
        var existing = await Context.ClipTombstones.AsNoTracking().FirstOrDefaultAsync(t => t.ConceptId == conceptId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            Context.ClipTombstones.Add(new ClipTombstone { ConceptId = conceptId, FirstCompletedAt = first.Value });
            await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (existing.FirstCompletedAt > first.Value)
        {
            await Context.ClipTombstones.Where(t => t.ConceptId == conceptId)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.FirstCompletedAt, first.Value), cancellationToken).ConfigureAwait(false);
        }

        var removed = await rows.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        Context.ChangeTracker.Clear();
        return removed > 0;
    }
}
