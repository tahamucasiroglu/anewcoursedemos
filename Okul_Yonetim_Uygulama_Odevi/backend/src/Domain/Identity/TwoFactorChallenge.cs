namespace OkulYonetim.Domain.Identity;

/// <summary>
/// Şifresi doğrulanmış platform yöneticisinin iki adımlı doğrulama beklemesi (spec 0007): 5 dakika, tek kullanımlık;
/// yalnızca özeti saklanır. Kurulumda henüz onaylanmamış (şifreli) anahtar burada bekler. Oturum, kod doğrulanınca açılır.
/// </summary>
public sealed class TwoFactorChallenge
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private TwoFactorChallenge(Guid id, Guid userId, string tokenHash, DateTimeOffset createdAt)
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

    public string? PendingSecretCipher { get; private set; }

    public static TwoFactorChallenge Create(Guid userId, string tokenHash, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), userId, tokenHash, now);

    public bool IsUsable(DateTimeOffset now) => this.UsedAt is null && now < this.ExpiresAt;

    public void SetPendingSecret(string secretCipher) => this.PendingSecretCipher = secretCipher;

    public void MarkUsed(DateTimeOffset now) => this.UsedAt ??= now;
}
