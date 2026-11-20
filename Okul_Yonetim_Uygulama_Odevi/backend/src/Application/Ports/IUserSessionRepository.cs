using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Ports;

/// <summary>Oturumlar: kimliğe ya da yenileme anahtarının özetine göre bulunur.</summary>
public interface IUserSessionRepository
{
    Task<UserSession?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<UserSession?> FindByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken);

    Task<UserSession?> FindByPreviousRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken);

    void Add(UserSession session);
}
