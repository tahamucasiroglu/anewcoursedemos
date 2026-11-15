namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// Rol adları (JWT <c>role</c> claim değerleri).
/// </summary>
public static class RoleNames
{
    /// <summary>Yönetici rolü: tüm izinlere sahiptir. Başlangıçta yapılandırmadan tohumlanan yönetici hesabı bu roldedir; kayıt olanlar normal kullanıcıdır.</summary>
    public const string Admin = "Admin";
}
