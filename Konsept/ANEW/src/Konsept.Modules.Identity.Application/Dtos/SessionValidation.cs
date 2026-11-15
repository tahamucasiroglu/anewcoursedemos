using Konsept.Modules.Identity.Domain.Enums;

namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>
/// Her Api isteğindeki oturum doğrulamasının sonucu (spec 0011, plan Risk 1): oturum geçerli mi ve kullanıcının o anki
/// rolü. Rol aynı sorguyla veritabanından okunur; JWT'de rol ya da yetki taşınmaz.
/// </summary>
/// <param name="IsValid">Oturum geçerli mi.</param>
/// <param name="Role">Kullanıcının rolü (geçersiz oturumda <see cref="Role.User"/>).</param>
public sealed record SessionValidation(bool IsValid, Role Role)
{
    /// <summary>Gets geçersiz oturum.</summary>
    public static SessionValidation Invalid { get; } = new(false, Role.User);
}
