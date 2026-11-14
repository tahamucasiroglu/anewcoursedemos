using Microsoft.AspNetCore.Identity;
using TahaKonsept.BuildingBlocks.Application.Security;
using TahaKonsept.Modules.Identity.Application.Base;
using TahaKonsept.Modules.Identity.Application.Models;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Concrete;

/// <summary>
/// İzin kuralı: <see cref="RoleNames.Admin"/> rolü → <see cref="PermissionNames.All"/>; diğer kullanıcılar → kendi
/// <c>permission</c> claim'leri (yalnızca tanımlı izin adları).
/// </summary>
public sealed class PermissionResolver : PermissionResolverBase
{
    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="users">Kullanıcı yöneticisi.</param>
    public PermissionResolver(UserManager<AppUser> users)
        : base(users)
    {
    }

    /// <inheritdoc />
    protected override UserAccess Combine(IReadOnlyList<string> roles, IReadOnlyList<(string Type, string Value)> claims)
    {
        var isAdmin = roles.Contains(RoleNames.Admin, StringComparer.Ordinal);
        var permissions = isAdmin
            ? PermissionNames.All
            : claims
                .Where(c => c.Type == CustomClaimTypes.Permission && PermissionNames.All.Contains(c.Value, StringComparer.Ordinal))
                .Select(c => c.Value);

        return new UserAccess(
            isAdmin,
            roles.Order(StringComparer.Ordinal).ToArray(),
            permissions.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
    }
}
