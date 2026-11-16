using Konsept.Modules.Identity.Domain.Enums;

namespace Konsept.Modules.Identity.Domain.Rules;

/// <summary>
/// Rol → model kullanma yetkileri (spec 0013 Req 2): rol için yapılandırılmış model listesi boşsa rol katalogdaki tüm modellere
/// yetkilidir (<see cref="Permissions.ModelUseAll"/>); doluysa yalnızca listedeki modellere (<c>model.use:&lt;id&gt;</c>).
/// Bilinmeyen rol hiçbir modele yetkili değildir (kapalı başarısızlık). Saf kural; karar her istekte sunucuda verilir.
/// </summary>
public static class RoleModelPermissions
{
    /// <summary>Rolün model yetkileri.</summary>
    /// <param name="role">Rol.</param>
    /// <param name="userModels">Kullanıcı rolüne yapılandırılmış modeller (boş → tümü).</param>
    /// <param name="adminModels">Admin rolüne yapılandırılmış modeller (boş → tümü).</param>
    /// <returns>Yetki adları.</returns>
    public static IReadOnlyList<string> For(Role role, IReadOnlyList<string>? userModels, IReadOnlyList<string>? adminModels)
    {
        if (role is not (Role.User or Role.Admin))
        {
            return [];
        }

        var configured = role == Role.Admin ? adminModels : userModels;
        return configured is null || configured.Count == 0
            ? [Permissions.ModelUseAll]
            : [.. configured.Select(m => Permissions.ModelUsePrefix + m)];
    }
}
