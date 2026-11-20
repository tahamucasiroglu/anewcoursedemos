using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Ports;

/// <summary>Oturumlar: kimliğe ya da yenileme anahtarının özetine göre bulunur.</summary>
public interface IUserSessionRepository
{
    Task<UserSession?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<UserSession?> FindByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken);

    Task<UserSession?> FindByPreviousRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken);

    /// <summary>Kişinin bütün açık oturumlarını iptal eder (kurucunun şifre sıfırlaması — spec 0005).</summary>
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Oturum hâlâ açık mı (izlemesiz, taze okuma).</summary>
    Task<bool> IsOpenAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Kişinin <paramref name="exceptSessionId"/> dışındaki bütün açık oturumlarını kapatır (spec 0006 şifre değiştirme).</summary>
    Task RevokeAllForUserExceptAsync(Guid userId, Guid exceptSessionId, DateTimeOffset now, CancellationToken cancellationToken);

    void Add(UserSession session);
}
