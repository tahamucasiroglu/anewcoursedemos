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

    public void Add(UserSession session) => this.context.UserSessions.Add(session);
}
