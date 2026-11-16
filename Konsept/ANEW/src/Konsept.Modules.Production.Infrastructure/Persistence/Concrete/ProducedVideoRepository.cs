using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>Video sürüm kayıtları deposu (spec 0017 Req 22): yalnızca ekleme ve okuma (kayıt değişmez); sahibine göre süzülür (BR-1).</summary>
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
            .Where(v => v.UserId == userId && v.ProfileId == profileId)
            .OrderByDescending(v => v.EndedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<ProducedVideo?> GetAsync(Guid userId, Guid videoId, CancellationToken cancellationToken) =>
        Context.Videos.AsNoTracking().FirstOrDefaultAsync(v => v.Id == videoId && v.UserId == userId, cancellationToken);
}
