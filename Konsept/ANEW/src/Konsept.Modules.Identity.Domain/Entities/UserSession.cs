using Konsept.Modules.Identity.Domain.Rules;

namespace Konsept.Modules.Identity.Domain.Entities;

/// <summary>
/// Bir giriş oturumu (spec 0002 Req 13, 15): kimliği JWT'nin <c>sid</c> claim'idir; dönen refresh token'ın yalnızca
/// SHA-256 hash'i tutulur. İptal edilmemiş ve son kullanımdan bu yana 14 gün geçmemişse etkindir.
/// </summary>
public sealed class UserSession
{
    private UserSession(Guid id, Guid userId, string refreshTokenHash, DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        RefreshTokenHash = refreshTokenHash;
        CreatedAt = createdAt;
        LastUsedAt = createdAt;
    }

    /// <summary>Gets oturum kimliği (UUIDv7, <c>sid</c>).</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets oturumun sahibi.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets geçerli refresh token'ın hash'i.</summary>
    public string RefreshTokenHash { get; private set; }

    /// <summary>Gets açılış zamanı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets son kullanım zamanı (UTC).</summary>
    public DateTimeOffset LastUsedAt { get; private set; }

    /// <summary>Gets iptal zamanı; iptal edilmemişse <c>null</c>.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>Yeni oturum açar.</summary>
    /// <param name="userId">Sahip.</param>
    /// <param name="refreshTokenHash">İlk refresh token'ın hash'i.</param>
    /// <param name="now">Açılış zamanı.</param>
    /// <returns>Oturum.</returns>
    public static UserSession Open(Guid userId, string refreshTokenHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshTokenHash);
        var utc = now.ToUniversalTime();
        return new UserSession(Guid.CreateVersion7(utc), userId, refreshTokenHash, utc);
    }

    /// <summary>Oturum şu an geçerli mi.</summary>
    /// <param name="now">Şimdiki zaman.</param>
    /// <returns>Geçerliyse <c>true</c>.</returns>
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && !SessionPolicy.IsExpired(LastUsedAt, now);

    /// <summary>Son kullanımı günceller (bellek içi uygulamalar için).</summary>
    /// <param name="now">Kullanım zamanı.</param>
    public void Touch(DateTimeOffset now) => LastUsedAt = now.ToUniversalTime();

    /// <summary>Refresh token'ı döndürür ve son kullanımı günceller (bellek içi uygulamalar için).</summary>
    /// <param name="newHash">Yeni token'ın hash'i.</param>
    /// <param name="now">Zaman.</param>
    public void Rotate(string newHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newHash);
        RefreshTokenHash = newHash;
        Touch(now);
    }

    /// <summary>Oturumu iptal eder (zaten iptalse değişmez).</summary>
    /// <param name="now">İptal zamanı.</param>
    public void Revoke(DateTimeOffset now) => RevokedAt ??= now.ToUniversalTime();
}
