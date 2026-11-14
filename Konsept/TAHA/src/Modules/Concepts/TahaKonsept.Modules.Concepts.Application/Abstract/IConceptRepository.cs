using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Domain.Entities;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Konsept deposu: genel <see cref="IRepository{TEntity}"/> + kullanıcıya göre filtreli sorgular ve istatistik sayımı.
/// "Tracked" metotlar değiştirilecek kayıtları takipli döndürür.
/// </summary>
public interface IConceptRepository : IRepository<Concept>
{
    /// <summary>Kullanıcının verilen durumlardaki konseptleri (takipsiz, en yeni önce).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="statuses">Durumlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Konseptler.</returns>
    Task<IReadOnlyList<Concept>> ListForUserAsync(Guid userId, IReadOnlyCollection<ConceptStatus> statuses, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcıya ait konsepti takipli getirir.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Konsept ya da null.</returns>
    Task<Concept?> GetForUserTrackedAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcıya ait konsepti takipsiz getirir.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Konsept ya da null.</returns>
    Task<Concept?> GetForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Tüm kullanıcılar için duruma göre konsept sayıları (yönetim paneli).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum → sayı.</returns>
    Task<IReadOnlyDictionary<ConceptStatus, int>> CountByStatusAsync(CancellationToken cancellationToken = default);
}
