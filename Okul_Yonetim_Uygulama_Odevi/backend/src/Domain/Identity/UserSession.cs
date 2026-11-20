namespace OkulYonetim.Domain.Identity;

/// <summary>
/// Oturum: girişle açılır, seçili kurumu ve yenileme anahtarının özetini (düz hâli değil) taşır. Son kullanımdan
/// itibaren <see cref="IdleLifetime"/> (7 gün) kullanılmazsa geçersizdir; her yenilemede anahtar değişir ve süre uzar.
/// Çıkışla iptal edilir; iptal edilmiş ya da kurumu değişmiş oturumun eski erişim anahtarı kabul edilmez (spec 0004).
/// Bir önceki anahtarın özeti ve değişme zamanı tutulur: aynı anda gelen yenilemeler (birden çok sekme) için eski anahtar
/// <see cref="RotationGrace"/> boyunca geçer; daha sonra gelirse anahtar çalınmış sayılır (review M-1).
/// </summary>
public sealed class UserSession
{
    public static readonly TimeSpan IdleLifetime = TimeSpan.FromDays(7);

    public static readonly TimeSpan RotationGrace = TimeSpan.FromSeconds(30);

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

    public string? PreviousRefreshTokenHash { get; private set; }

    public DateTimeOffset? RotatedAt { get; private set; }

    public string? CreatedIp { get; private set; }

    public static UserSession Open(Guid userId, Guid? tenantId, string refreshTokenHash, DateTimeOffset now, string? ip) =>
        new(Guid.CreateVersion7(now), userId, tenantId, refreshTokenHash, now, ip);

    public bool IsActive(DateTimeOffset now) => this.RevokedAt is null && now - this.LastUsedAt < IdleLifetime;

    public void Rotate(string newRefreshTokenHash, DateTimeOffset now)
    {
        this.PreviousRefreshTokenHash = this.RefreshTokenHash;
        this.RotatedAt = now;
        this.RefreshTokenHash = newRefreshTokenHash;
        this.LastUsedAt = now;
    }

    /// <summary>Verilen özet bir önceki anahtarınsa ve değişimin üzerinden <see cref="RotationGrace"/> geçmediyse doğru.</summary>
    public bool IsWithinRotationGrace(string refreshTokenHash, DateTimeOffset now) =>
        this.PreviousRefreshTokenHash == refreshTokenHash && this.RotatedAt is { } rotatedAt && now - rotatedAt < RotationGrace;

    public void SelectTenant(Guid tenantId, DateTimeOffset now)
    {
        this.TenantId = tenantId;
        this.LastUsedAt = now;
    }

    public void Revoke(DateTimeOffset now) => this.RevokedAt ??= now;
}
