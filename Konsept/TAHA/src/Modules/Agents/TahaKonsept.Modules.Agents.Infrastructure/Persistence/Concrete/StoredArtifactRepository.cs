using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Domain.Entities;

namespace TahaKonsept.Modules.Agents.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IStoredArtifactRepository"/> EF uygulaması.
/// </summary>
public sealed class StoredArtifactRepository : Repository<StoredArtifact>, IStoredArtifactRepository
{
    /// <summary>Oluşturur.</summary>
    /// <param name="context">DbContext.</param>
    public StoredArtifactRepository(AgentsDbContext context)
        : base(context)
    {
    }

    /// <summary>İşin artefaktları.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıtlar.</returns>
    public async Task<IReadOnlyList<StoredArtifact>> ListByJobAsync(Guid userId, Guid jobId, CancellationToken cancellationToken = default) =>
        await Query().Where(a => a.UserId == userId && a.JobId == jobId).ToListAsync(cancellationToken);
}
