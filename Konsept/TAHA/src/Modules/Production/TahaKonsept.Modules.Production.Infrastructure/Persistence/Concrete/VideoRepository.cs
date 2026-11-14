using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IVideoRepository"/>'nin EF Core uygulaması: kullanıcının videoları, konsept sürümü ve istatistik zamanları.
/// </summary>
/// <param name="context">Modül DbContext'i.</param>
public sealed class VideoRepository(ProductionDbContext context) : Repository<Video>(context), IVideoRepository
{
    /// <summary>Kullanıcının videoları (yeniden eskiye, takipsiz).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="take">Azami adet.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Videolar.</returns>
    public async Task<IReadOnlyList<Video>> ListForUserAsync(Guid userId, int take, CancellationToken cancellationToken) =>
        await QueryNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAtUtc).Take(take).ToListAsync(cancellationToken);

    /// <summary>Kullanıcının videosu (takipli).</summary>
    /// <param name="id">Video.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Video ya da <c>null</c>.</returns>
    public Task<Video?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        Query().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

    /// <summary>Konseptin en büyük sürümü.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sürüm (yoksa 0).</returns>
    public async Task<int> MaxVersionForConceptAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken) =>
        await QueryNoTracking().Where(x => x.UserId == userId && x.ConceptId == conceptId).Select(x => (int?)x.Version).MaxAsync(cancellationToken) ?? 0;

    /// <summary>Verilen andan sonraki video zamanları.</summary>
    /// <param name="fromUtc">Başlangıç.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Zamanlar.</returns>
    public async Task<IReadOnlyList<DateTimeOffset>> ListCreatedSinceAsync(DateTimeOffset fromUtc, CancellationToken cancellationToken) =>
        await QueryNoTracking().Where(x => x.CreatedAtUtc >= fromUtc).Select(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
}
