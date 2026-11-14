namespace TahaKonsept.Modules.Identity.Application.Models;

/// <summary>
/// Kullanıcının çözülmüş erişim bilgisi (<see cref="Abstract.IPermissionResolver"/> sonucu).
/// </summary>
/// <param name="IsAdmin">Yönetici mi.</param>
/// <param name="Roles">Roller.</param>
/// <param name="Permissions">Etkin izinler (sıralı, tekrarsız).</param>
public sealed record UserAccess(bool IsAdmin, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);
