using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Identity.Domain.Rules;

namespace Konsept.Modules.Identity.Domain.Entities;

/// <summary>
/// Kayıtlı içerik üreticisi (spec 0002 Req 1–8): ad, e-posta (+ normalleştirilmiş biçimi, tekil), şifre hash'i (düz metin
/// asla), kilit sayacı ve zamanları (UTC), rolü (spec 0011; varsayılan <see cref="Role.User"/>).
/// </summary>
public sealed class User
{
    /// <summary>Ad alanının en fazla uzunluğu.</summary>
    public const int NameMaxLength = 100;

    /// <summary>E-posta alanının en fazla uzunluğu.</summary>
    public const int EmailMaxLength = 254;

    private User(Guid id, string name, string email, string normalizedEmail, string passwordHash, DateTimeOffset registeredAt, Role role)
    {
        Id = id;
        Name = name;
        Email = email;
        NormalizedEmail = normalizedEmail;
        PasswordHash = passwordHash;
        RegisteredAt = registeredAt;
        Role = role;
    }

    /// <summary>Gets kullanıcı kimliği (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets ad (kırpılmış).</summary>
    public string Name { get; private set; }

    /// <summary>Gets girildiği biçimiyle e-posta (kırpılmış).</summary>
    public string Email { get; private set; }

    /// <summary>Gets karşılaştırma biçimi (<see cref="EmailNormalizer"/>); tekil.</summary>
    public string NormalizedEmail { get; private set; }

    /// <summary>Gets şifre hash'i.</summary>
    public string PasswordHash { get; private set; }

    /// <summary>Gets ardışık yanlış deneme sayısı.</summary>
    public int FailedLoginCount { get; private set; }

    /// <summary>Gets kilidin açılma zamanı; kilitli değilse <c>null</c> ya da geçmiş.</summary>
    public DateTimeOffset? LockedUntil { get; private set; }

    /// <summary>Gets kayıt zamanı (UTC).</summary>
    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>Gets son başarılı giriş zamanı (UTC).</summary>
    public DateTimeOffset? LastLoginAt { get; private set; }

    /// <summary>Gets rol (spec 0011); yalnızca <see cref="AdminEmailPolicy"/> kararıyla belirlenir.</summary>
    public Role Role { get; private set; }

    /// <summary>Yeni kullanıcı oluşturur.</summary>
    /// <param name="name">Ad (kırpılır).</param>
    /// <param name="email">E-posta (kırpılır).</param>
    /// <param name="passwordHash">Şifre hash'i.</param>
    /// <param name="registeredAt">Kayıt zamanı.</param>
    /// <param name="role">Rol (yalnızca <see cref="AdminEmailPolicy"/> kararından; varsayılan kullanıcı).</param>
    /// <returns>Kullanıcı.</returns>
    public static User Register(string name, string email, string passwordHash, DateTimeOffset registeredAt, Role role = Role.User)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        var utc = registeredAt.ToUniversalTime();
        return new User(Guid.CreateVersion7(utc), name.Trim(), email.Trim(), EmailNormalizer.Normalize(email), passwordHash, utc, role);
    }

    /// <summary>Hesap şu an kilitli mi.</summary>
    /// <param name="now">Şimdiki zaman.</param>
    /// <returns>Kilitliyse <c>true</c>.</returns>
    public bool IsLocked(DateTimeOffset now) => LockoutPolicy.IsLocked(LockedUntil, now);

    /// <summary>Yanlış denemeyi kurala göre uygular (bellek içi uygulamalar için; veritabanı atomik SQL kullanır).</summary>
    /// <param name="now">Deneme zamanı.</param>
    /// <returns>Sonuç.</returns>
    public LockoutOutcome ApplyFailedAttempt(DateTimeOffset now)
    {
        var outcome = LockoutPolicy.Evaluate(FailedLoginCount, LockedUntil, now);
        FailedLoginCount = outcome.FailedCount;
        LockedUntil = outcome.LockedUntil;
        return outcome;
    }

    /// <summary>
    /// Rolü ayardaki admin e-postasına göre eşitler (açılış senkronunun bellek içi karşılığı; veritabanı tek ifadelik
    /// <c>UPDATE</c> kullanır): eşleşen hesap admin, diğer her hesap kullanıcı olur.
    /// </summary>
    /// <param name="normalizedAdminEmail">Ayardaki e-postanın normalleştirilmiş biçimi; ayar boşsa <c>null</c>.</param>
    /// <returns>Rol değiştiyse <c>true</c>.</returns>
    public bool SyncAdminRole(string? normalizedAdminEmail)
    {
        var role = AdminEmailPolicy.RoleForNormalized(normalizedAdminEmail, NormalizedEmail);
        if (role == Role)
        {
            return false;
        }

        Role = role;
        return true;
    }

    /// <summary>Başarılı girişi uygular: kilitliyse hiçbir şey değişmez.</summary>
    /// <param name="now">Giriş zamanı.</param>
    /// <returns>Uygulandıysa <c>true</c>.</returns>
    public bool ApplySuccessfulLogin(DateTimeOffset now)
    {
        if (IsLocked(now))
        {
            return false;
        }

        FailedLoginCount = 0;
        LastLoginAt = now.ToUniversalTime();
        return true;
    }
}
