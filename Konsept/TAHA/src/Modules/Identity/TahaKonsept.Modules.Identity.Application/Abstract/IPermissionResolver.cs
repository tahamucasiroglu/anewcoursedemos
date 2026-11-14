using TahaKonsept.Modules.Identity.Application.Models;
using TahaKonsept.Modules.Identity.Domain.Entities;

namespace TahaKonsept.Modules.Identity.Application.Abstract;

/// <summary>
/// Kullanıcının etkin rol ve izinlerini çözer: <c>Admin</c> rolü tüm izinlere sahiptir; diğerleri kullanıcı
/// <c>permission</c> claim'lerinden.
/// </summary>
public interface IPermissionResolver
{
    /// <summary>Rol ve izinleri çözer.</summary>
    /// <param name="user">Kullanıcı.</param>
    /// <returns>Erişim bilgisi.</returns>
    Task<UserAccess> ResolveAsync(AppUser user);
}
