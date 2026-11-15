using Konsept.Modules.Identity.Domain.Enums;

namespace Konsept.Modules.Identity.Domain.Rules;

/// <summary>
/// Rol → yetki eşlemesi (spec 0011 Req 2): admin <see cref="Permissions.AdminPanel"/>'i taşır, kullanıcı hiçbir yetki
/// taşımaz. Saf kural; yetki kararı her istekte bu eşlemeyle sunucuda verilir.
/// </summary>
public static class RolePermissions
{
    private static readonly IReadOnlyList<string> None = [];

    private static readonly IReadOnlyList<string> AdminPermissions = [Permissions.AdminPanel];

    /// <summary>Rolün yetkileri.</summary>
    /// <param name="role">Rol.</param>
    /// <returns>Yetki adları; bilinmeyen rol için boş liste (kapalı başarısızlık).</returns>
    public static IReadOnlyList<string> For(Role role) => role switch
    {
        Role.Admin => AdminPermissions,
        _ => None,
    };
}
