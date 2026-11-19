namespace OkulYonetim.Domain.Identity;

/// <summary>
/// Bir e-posta için art arda hatalı giriş sayacı ve kilit (spec 0004, gereksinim 7): <see cref="MaxFailures"/> (3) hatada
/// <see cref="LockDuration"/> (15 dk) kilit; kilit süresince doğru şifre de reddedilir; başarılı giriş sayacı sıfırlar.
/// Sistemde olmayan e-postalar için de tutulur — böylece kimin kayıtlı olduğu anlaşılmaz.
/// </summary>
public sealed class LoginThrottle
{
    public const int MaxFailures = 3;
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    private LoginThrottle(string email)
    {
        this.Email = email;
    }

    public string Email { get; private set; }

    public int FailedCount { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public static LoginThrottle For(string email) => new(EmailAddress.Normalize(email));

    public bool IsLocked(DateTimeOffset now) => this.LockedUntil is { } until && now < until;

    /// <summary>Hatalı denemeyi sayar; sayı sınıra ulaşınca kilitler ve true döner.</summary>
    public bool RegisterFailure(DateTimeOffset now)
    {
        if (this.LockedUntil is { } until && now >= until)
        {
            this.FailedCount = 0;
            this.LockedUntil = null;
        }

        this.FailedCount++;
        if (this.FailedCount >= MaxFailures)
        {
            this.LockedUntil = now + LockDuration;
            return true;
        }

        return false;
    }

    public void Reset()
    {
        this.FailedCount = 0;
        this.LockedUntil = null;
    }
}
