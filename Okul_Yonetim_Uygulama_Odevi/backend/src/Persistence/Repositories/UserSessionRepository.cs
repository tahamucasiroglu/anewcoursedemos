using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Oturum deposu (kurum filtresi yok; oturum kişiye aittir).</summary>
public sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly OkulDbContext context;

    public UserSessionRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<UserSession?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.UserSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<UserSession?> FindByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken) =>
        this.context.UserSessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash, cancellationToken);

    public Task<UserSession?> FindByPreviousRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken) =>
        this.context.UserSessions.FirstOrDefaultAsync(s => s.PreviousRefreshTokenHash == refreshTokenHash, cancellationToken);

    public async Task RevokeAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        List<UserSession> open = await this.context.UserSessions.Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (UserSession session in open)
        {
            session.Revoke(now);
        }
    }

    public async Task RevokeAllForUserExceptAsync(Guid userId, Guid exceptSessionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        List<UserSession> open = await this.context.UserSessions.Where(s => s.UserId == userId && s.Id != exceptSessionId && s.RevokedAt == null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (UserSession session in open)
        {
            session.Revoke(now);
        }
    }

    public void Add(UserSession session) => this.context.UserSessions.Add(session);
}
