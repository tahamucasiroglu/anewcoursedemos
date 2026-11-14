using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Application.Abstract;

/// <summary>
/// Diğer modüllerin (Concepts, Production…) profil okuması için PUBLIC sözleşme. Kullanıcı bağlamı gerektirmez
/// (arka plan işlerinde de çalışır). <see cref="GetActiveAsync"/>: kullanıcının aktif profili; <see cref="GetAsync"/>: kimlikle profil.
/// </summary>
public interface IProfileReader
{
    /// <summary>Kullanıcının aktif profilini getirir.</summary>
    /// <param name="userId">Kullanıcı kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Aktif profil ya da yoksa <c>null</c>.</returns>
    Task<ProfileDto?> GetActiveAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Kimliğe göre profil getirir.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da yoksa <c>null</c>.</returns>
    Task<ProfileDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
