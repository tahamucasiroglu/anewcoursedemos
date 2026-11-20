namespace OkulYonetim.Domain.Identity;

/// <summary>
/// Kişi (giriş hesabı): sistemde tek kimliktir — aynı e-posta birden çok kurumda rol alabilir (spec 0004, gereksinim 2).
/// E-posta karşılaştırma biçiminde (<see cref="EmailAddress"/>) saklanır; şifre yalnızca tek yönlü özet olarak durur.
/// </summary>
public sealed class User
{
    private User(Guid id, string email, string fullName, string passwordHash, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.Email = email;
        this.FullName = fullName;
        this.PasswordHash = passwordHash;
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string FullName { get; private set; }

    public string PasswordHash { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string? Phone { get; private set; }

    /// <summary>Platform yöneticisi (spec 0007): bütün okulların özetini görür; yalnızca komutla verilir/alınır.</summary>
    public bool IsPlatformAdmin { get; private set; }

    /// <summary>İki adımlı doğrulama anahtarı, şifrelenmiş (düz anahtar saklanmaz).</summary>
    public string? TwoFactorSecretCipher { get; private set; }

    public DateTimeOffset? TwoFactorEnabledAt { get; private set; }

    /// <summary>Son kabul edilen kodun 30 sn'lik dilimi: aynı kod ikinci kez kabul edilmez.</summary>
    public long? TwoFactorLastStep { get; private set; }

    public static User Create(string email, string fullName, string passwordHash, DateTimeOffset now, string? phone = null) =>
        new(Guid.CreateVersion7(now), EmailAddress.Normalize(email), fullName.Trim(), passwordHash, now) { Phone = phone };

    /// <summary>Kurucunun düzenleyebildiği bilgiler (spec 0005): ad-soyad ve telefon. E-posta kimliktir, değişmez.</summary>
    public void UpdateProfile(string fullName, string? phone)
    {
        this.FullName = fullName.Trim();
        this.Phone = phone;
    }

    /// <summary>Şifre özetini değiştirir (kurucunun sıfırlaması — spec 0005); düz şifre alınmaz.</summary>
    public void ChangePasswordHash(string passwordHash) => this.PasswordHash = passwordHash;

    public void GrantPlatformAdmin() => this.IsPlatformAdmin = true;

    public void RevokePlatformAdmin() => this.IsPlatformAdmin = false;

    /// <summary>Kurulum tamamlandı: şifreli anahtar ve kurulumda kullanılan kodun dilimi.</summary>
    public void EnableTwoFactor(string secretCipher, DateTimeOffset now, long step)
    {
        this.TwoFactorSecretCipher = secretCipher;
        this.TwoFactorEnabledAt = now;
        this.TwoFactorLastStep = step;
    }

    /// <summary>Kurtarma (komutla): doğrulama kaldırılır, sonraki girişte yeniden kurulur.</summary>
    public void ResetTwoFactor()
    {
        this.TwoFactorSecretCipher = null;
        this.TwoFactorEnabledAt = null;
        this.TwoFactorLastStep = null;
    }

    /// <summary>Kod dilimi bir öncekinden büyükse kabul eder ve kaydeder; aynı ya da eski dilim reddedilir.</summary>
    public bool AcceptTwoFactorStep(long step)
    {
        if (this.TwoFactorLastStep is { } last && step <= last)
        {
            return false;
        }

        this.TwoFactorLastStep = step;
        return true;
    }
}
