namespace OkulYonetim.Domain.Identity;

/// <summary>
/// "Şifremi unuttum" yenileme bağlantısı (spec 0006): 30 dakika geçerli, tek kullanımlık. Bağlantının kendisi değil yalnızca
/// tek yönlü özeti saklanır. Yeni bağlantı istenince ya da şifre herhangi bir yolla değişince açıkta kalanlar geçersizleşir.
/// </summary>
public sealed class PasswordResetToken
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private PasswordResetToken(Guid id, Guid userId, string tokenHash, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.UserId = userId;
        this.TokenHash = tokenHash;
        this.CreatedAt = createdAt;
        this.ExpiresAt = createdAt + Lifetime;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public static PasswordResetToken Create(Guid userId, string tokenHash, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), userId, tokenHash, now);

    public bool IsUsable(DateTimeOffset now) => this.UsedAt is null && this.RevokedAt is null && now < this.ExpiresAt;

    public void MarkUsed(DateTimeOffset now) => this.UsedAt ??= now;

    public void Revoke(DateTimeOffset now) => this.RevokedAt ??= now;
}
