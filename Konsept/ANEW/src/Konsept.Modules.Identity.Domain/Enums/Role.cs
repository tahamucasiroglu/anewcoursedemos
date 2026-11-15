namespace Konsept.Modules.Identity.Domain.Enums;

/// <summary>
/// Kullanıcının rolü (spec 0011 Req 1): her yeni hesap <see cref="User"/> rolüyle açılır; <see cref="Admin"/> yalnızca
/// ortam ayarındaki e-postayla belirlenir (<see cref="Rules.AdminEmailPolicy"/>). Rol, yetkilerin paketidir
/// (<see cref="Rules.RolePermissions"/>).
/// </summary>
public enum Role
{
    /// <summary>Varsayılan kullanıcı rolü; admin alanı yetkisi yok.</summary>
    User = 0,

    /// <summary>Admin rolü; <c>admin.panel</c> yetkisini taşır.</summary>
    Admin = 1,
}
