using TahaKonsept.Modules.Identity.Domain.Entities;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Kullanıcılar üzerinde salt okunur sorgular (EF'e bağımlı olmadan async sayım/listeleme). Yazma işleri <c>UserManager</c> ile yapılır.
/// </summary>
public interface IUserReadRepository
{
    /// <summary>Kullanıcı sayısı.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sayı.</returns>
    Task<int> CountAsync(CancellationToken ct = default);

    /// <summary>Tüm kullanıcılar (izlenmeyen, ada göre sıralı).</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    Task<IReadOnlyList<AppUser>> ListAsync(CancellationToken ct = default);

    /// <summary>Belirtilen roldeki yasaklı olmayan kullanıcı sayısı.</summary>
    /// <param name="roleName">Rol adı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sayı.</returns>
    Task<int> CountActiveInRoleAsync(string roleName, CancellationToken ct = default);

    /// <summary>Belirtilen roldeki kullanıcı kimlikleri.</summary>
    /// <param name="roleName">Rol adı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kimlikler.</returns>
    Task<IReadOnlySet<Guid>> ListUserIdsInRoleAsync(string roleName, CancellationToken ct = default);
}
