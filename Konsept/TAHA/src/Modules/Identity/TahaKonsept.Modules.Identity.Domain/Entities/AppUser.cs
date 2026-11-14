using Microsoft.AspNetCore.Identity;
using TahaKonsept.Modules.Identity.Domain.Enums;

namespace TahaKonsept.Modules.Identity.Domain.Entities;

/// <summary>
/// Uygulama kullanıcısı (ASP.NET Core Identity, Guid v7 anahtar, PBKDF2 parola). Ek alanlar:
/// <see cref="IsBanned"/> (yasaklı kullanıcı giriş/yenileme yapamaz), <see cref="Theme"/> (tema tercihi),
/// <see cref="CreatedAtUtc"/>, <see cref="LastLoginAtUtc"/>. Davranışlar: <see cref="Ban"/>, <see cref="Unban"/>,
/// <see cref="SetTheme"/>, <see cref="RecordLogin"/>.
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    /// <summary>EF için boş oluşturucu.</summary>
    public AppUser()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>Yeni kullanıcı oluşturur.</summary>
    /// <param name="userName">Kullanıcı adı.</param>
    /// <param name="createdAtUtc">Oluşturulma anı (UTC).</param>
    public AppUser(string userName, DateTimeOffset createdAtUtc)
        : base(userName)
    {
        Id = Guid.CreateVersion7();
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Yasaklı mı.</summary>
    public bool IsBanned { get; private set; }

    /// <summary>Yasaklanma anı (UTC).</summary>
    public DateTimeOffset? BannedAtUtc { get; private set; }

    /// <summary>Tema tercihi.</summary>
    public UserTheme Theme { get; private set; } = UserTheme.System;

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Son başarılı giriş anı (UTC).</summary>
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    /// <summary>Kullanıcıyı yasaklar.</summary>
    /// <param name="nowUtc">Şu an (UTC).</param>
    public void Ban(DateTimeOffset nowUtc)
    {
        IsBanned = true;
        BannedAtUtc = nowUtc;
    }

    /// <summary>Yasağı kaldırır.</summary>
    public void Unban()
    {
        IsBanned = false;
        BannedAtUtc = null;
    }

    /// <summary>Tema tercihini değiştirir.</summary>
    /// <param name="theme">Yeni tema.</param>
    public void SetTheme(UserTheme theme) => Theme = theme;

    /// <summary>Başarılı girişi kaydeder.</summary>
    /// <param name="nowUtc">Şu an (UTC).</param>
    public void RecordLogin(DateTimeOffset nowUtc) => LastLoginAtUtc = nowUtc;
}
