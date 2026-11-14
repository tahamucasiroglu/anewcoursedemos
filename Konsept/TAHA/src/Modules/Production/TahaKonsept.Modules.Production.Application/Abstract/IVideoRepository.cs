using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Üretilen videolar deposu.
/// </summary>
public interface IVideoRepository : IRepository<Video>
{
    /// <summary>Kullanıcının videoları (yeniden eskiye, takipsiz).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="take">Azami adet.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<IReadOnlyList<Video>> ListForUserAsync(Guid userId, int take, CancellationToken cancellationToken);

    /// <summary>Kullanıcının videosu (takipli).</summary>
    /// <param name="id">Video.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Video?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>Konseptin en büyük video sürümü (yoksa 0).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<int> MaxVersionForConceptAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken);

    /// <summary>Verilen andan sonra oluşturulan videoların zamanları (tüm kullanıcılar).</summary>
    /// <param name="fromUtc">Başlangıç.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<IReadOnlyList<DateTimeOffset>> ListCreatedSinceAsync(DateTimeOffset fromUtc, CancellationToken cancellationToken);
}
