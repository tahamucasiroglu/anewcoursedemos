using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Ports;

/// <summary>İki adımlı doğrulama beklemeleri (spec 0007).</summary>
public interface ITwoFactorChallengeRepository
{
    Task<TwoFactorChallenge?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<Guid?> FindOwnerIdAsync(string tokenHash, CancellationToken cancellationToken);

    void Add(TwoFactorChallenge challenge);
}
