using Microsoft.AspNetCore.Identity;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Models;
using TahaKonsept.Modules.Identity.Domain.Entities;

namespace TahaKonsept.Modules.Identity.Application.Base;

/// <summary>
/// <see cref="IPermissionResolver"/> tabanı: rolleri ve kullanıcı claim'lerini <see cref="UserManager{TUser}"/>'dan okur;
/// izin birleştirme kuralı türetilen sınıfta (<see cref="Combine"/>).
/// </summary>
public abstract class PermissionResolverBase : IPermissionResolver
{
    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="users">Kullanıcı yöneticisi.</param>
    protected PermissionResolverBase(UserManager<AppUser> users)
    {
        Users = users ?? throw new ArgumentNullException(nameof(users));
    }

    /// <summary>Kullanıcı yöneticisi.</summary>
    protected UserManager<AppUser> Users { get; }

    /// <summary>Rol ve izinleri çözer.</summary>
    /// <param name="user">Kullanıcı.</param>
    /// <returns>Erişim bilgisi.</returns>
    public async Task<UserAccess> ResolveAsync(AppUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var roles = (await Users.GetRolesAsync(user)).ToArray();
        var claims = await Users.GetClaimsAsync(user);
        return Combine(roles, claims.Select(c => (c.Type, c.Value)).ToArray());
    }

    /// <summary>Roller ve kullanıcı claim'lerinden etkin erişimi hesaplar.</summary>
    /// <param name="roles">Roller.</param>
    /// <param name="claims">Kullanıcı claim'leri (tür, değer).</param>
    /// <returns>Erişim bilgisi.</returns>
    protected abstract UserAccess Combine(IReadOnlyList<string> roles, IReadOnlyList<(string Type, string Value)> claims);
}
