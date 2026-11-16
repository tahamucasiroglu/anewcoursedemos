using Konsept.Modules.Profiles.Domain.Entities;

namespace Konsept.Modules.Profiles.Application.Abstract;

/// <summary>
/// Profillerin ve seçili profilin kalıcılık yüzeyi. Her işlem kullanıcı kimliğiyle süzülür (BR-1, AC-15): başka kullanıcının
/// profili hiçbir yoldan okunmaz, değişmez, silinmez.
/// </summary>
public interface IProfileRepository
{
    /// <summary>Kullanıcının profilleri.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profiller (sırasız).</returns>
    Task<IReadOnlyList<Profile>> ListAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Kullanıcının bir profili.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil; yoksa ya da başkasınınsa <c>null</c>.</returns>
    Task<Profile?> GetAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);

    /// <summary>Profil ekler.</summary>
    /// <param name="profile">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ad anahtarı aynı kullanıcıda zaten varsa <c>false</c>.</returns>
    Task<bool> AddAsync(Profile profile, CancellationToken cancellationToken);

    /// <summary>Kullanıcının profilini günceller (son kaydeden kazanır, Req 15).</summary>
    /// <param name="profile">Yeni değerler (kimlik + sahip aynı).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ad anahtarı başka profilde varsa <c>false</c>.</returns>
    Task<bool> UpdateAsync(Profile profile, CancellationToken cancellationToken);

    /// <summary>Kullanıcının profilini kalıcı siler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silindiyse <c>true</c>.</returns>
    Task<bool> DeleteAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);

    /// <summary>Hatırlanan seçili profil.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Profil kimliği ya da <c>null</c>.</returns>
    Task<Guid?> GetSelectionAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Seçili profili hatırlar (kullanıcı başına tek satır).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task SetSelectionAsync(Guid userId, Guid profileId, CancellationToken cancellationToken);
}
