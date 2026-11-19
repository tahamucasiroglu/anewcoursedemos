namespace OkulYonetim.Domain.Identity;

/// <summary>
/// Oturum: girişle açılır, seçili kurumu ve yenileme anahtarının özetini (düz hâli değil) taşır. Son kullanımdan
/// itibaren <see cref="IdleLifetime"/> (7 gün) kullanılmazsa geçersizdir; her yenilemede anahtar değişir ve süre uzar.
/// Çıkışla iptal edilir; iptal edilmiş ya da kurumu değişmiş oturumun eski erişim anahtarı kabul edilmez (spec 0004).
/// </summary>
public sealed class UserSession
{
    public static readonly TimeSpan IdleLifetime = TimeSpan.FromDays(7);

    private UserSession(Guid id, Guid userId, Guid? tenantId, string refreshTokenHash, DateTimeOffset createdAt, string? createdIp)
    {
        this.Id = id;
        this.UserId = userId;
        this.TenantId = tenantId;
        this.RefreshTokenHash = refreshTokenHash;
        this.CreatedAt = createdAt;
        this.LastUsedAt = createdAt;
        this.CreatedIp = createdIp;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid? TenantId { get; private set; }

    public string RefreshTokenHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastUsedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? CreatedIp { get; private set; }

    public static UserSession Open(Guid userId, Guid? tenantId, string refreshTokenHash, DateTimeOffset now, string? ip) =>
        new(Guid.CreateVersion7(now), userId, tenantId, refreshTokenHash, now, ip);

    public bool IsActive(DateTimeOffset now) => this.RevokedAt is null && now - this.LastUsedAt < IdleLifetime;

    public void Rotate(string newRefreshTokenHash, DateTimeOffset now)
    {
        this.RefreshTokenHash = newRefreshTokenHash;
        this.LastUsedAt = now;
    }

    public void SelectTenant(Guid tenantId, DateTimeOffset now)
    {
        this.TenantId = tenantId;
        this.LastUsedAt = now;
    }

    public void Revoke(DateTimeOffset now) => this.RevokedAt ??= now;
}
