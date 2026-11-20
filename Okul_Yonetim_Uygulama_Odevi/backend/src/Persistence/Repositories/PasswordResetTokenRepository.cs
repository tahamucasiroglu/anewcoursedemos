using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Şifre yenileme bağlantıları deposu (spec 0006).</summary>
public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly OkulDbContext context;

    public PasswordResetTokenRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        this.context.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public Task<int> CountCreatedSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken) =>
        this.context.PasswordResetTokens.CountAsync(t => t.UserId == userId && t.CreatedAt > since, cancellationToken);

    public async Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        List<PasswordResetToken> open = await this.context.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedAt == null && t.RevokedAt == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (PasswordResetToken token in open)
        {
            token.Revoke(now);
        }
    }

    public void Add(PasswordResetToken token) => this.context.PasswordResetTokens.Add(token);
}
