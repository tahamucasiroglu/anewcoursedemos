using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Ports;

/// <summary>Şifre yenileme bağlantıları (spec 0006); kişiye aittir, okul filtresi yoktur.</summary>
public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Kişi için <paramref name="since"/> sonrasında üretilen bağlantı sayısı (e-posta başına sınır).</summary>
    Task<int> CountCreatedSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>Kişinin açıkta kalan bütün bağlantılarını geçersiz kılar.</summary>
    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    void Add(PasswordResetToken token);
}
