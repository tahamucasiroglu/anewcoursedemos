using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>İki adımlı doğrulama beklemeleri deposu (spec 0007).</summary>
public sealed class TwoFactorChallengeRepository : ITwoFactorChallengeRepository
{
    private readonly OkulDbContext context;

    public TwoFactorChallengeRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<TwoFactorChallenge?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        this.context.TwoFactorChallenges.FirstOrDefaultAsync(c => c.TokenHash == tokenHash, cancellationToken);

    public Task<Guid?> FindOwnerIdAsync(string tokenHash, CancellationToken cancellationToken) =>
        this.context.TwoFactorChallenges.AsNoTracking().Where(c => c.TokenHash == tokenHash).Select(c => (Guid?)c.UserId).FirstOrDefaultAsync(cancellationToken);

    public void Add(TwoFactorChallenge challenge) => this.context.TwoFactorChallenges.Add(challenge);
}
