using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Profiles.Application.Abstract;

/// <summary>
/// Diğer modüllerin (Concepts, Production…) profil okuması için PUBLIC sözleşme. Kullanıcı bağlamı gerektirmez
/// (arka plan işlerinde de çalışır; çağıran sahibin kimliğini verir). <see cref="GetActiveAsync"/>: kullanıcının aktif profili;
/// <see cref="GetAsync"/>: kimlikle profil (yalnız o kullanıcıya aitse; başkasınınki <c>null</c> döner).
/// </summary>
public interface IProfileReader
{
    /// <summary>Kullanıcının aktif profilini getirir.</summary>
    /// <param name="userId">Kullanıcı kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Aktif profil ya da yoksa <c>null</c>.</returns>
    Task<ProfileDto?> GetActiveAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcıya ait profili kimliğe göre getirir.</summary>
    /// <param name="userId">Sahip kullanıcı.</param>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil ya da yoksa/başkasınınsa <c>null</c>.</returns>
    Task<ProfileDto?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}
