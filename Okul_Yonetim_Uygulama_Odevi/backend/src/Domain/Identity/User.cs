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
}
