using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Profiles.Domain.Entities;

namespace TahaKonsept.Modules.Profiles.Application.Abstract;

/// <summary>
/// Profil deposu: genel <see cref="IRepository{TEntity}"/> + kullanıcıya göre filtrelenmiş sorgular.
/// "Tracked" metotlar değiştirilecek kayıtları takipli döndürür; diğerleri takipsizdir.
/// </summary>
public interface IProfileRepository : IRepository<Profile>
{
    /// <summary>Kullanıcının tüm profilleri (takipsiz, oluşturulma sırasıyla).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profiller.</returns>
    Task<IReadOnlyList<Profile>> ListForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcıya ait profili takipli getirir.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da <c>null</c>.</returns>
    Task<Profile?> GetForUserTrackedAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının aktif işaretli profillerini takipli getirir (normalde en fazla bir tane).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Aktif profiller.</returns>
    Task<IReadOnlyList<Profile>> ListActiveTrackedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Verilen profil dışındaki en yeni profili takipli getirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="excludeId">Hariç tutulacak profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da <c>null</c>.</returns>
    Task<Profile?> GetNewestOtherTrackedAsync(Guid userId, Guid excludeId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının aktif profilini takipsiz getirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da <c>null</c>.</returns>
    Task<Profile?> GetActiveAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcıya ait profili kimliğe göre takipsiz getirir.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da <c>null</c>.</returns>
    Task<Profile?> FindForUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
