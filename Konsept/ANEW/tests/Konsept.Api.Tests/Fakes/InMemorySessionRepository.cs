using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Enums;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Hızlı profil için bellekte oturum deposu; her işlem tek kilit altında (gerçek deponun tek ifadelik UPDATE'lerinin
/// eşdeğeri): yalnızca etkin oturumun refresh token'ı döner, eski token ikinci kez dönmez.
/// </summary>
/// <param name="users">Rolün okunacağı kullanıcı deposu (gerçek deponun <c>JOIN users</c> karşılığı); verilmezse her oturumun
/// rolü kullanıcıdır.</param>
public sealed class InMemorySessionRepository(InMemoryUserRepository? users = null) : ISessionRepository
{
    private readonly Lock _gate = new();
    private readonly List<UserSession> _sessions = [];

    /// <summary>Gets oturumların anlık kopyası.</summary>
    public IReadOnlyList<UserSession> Sessions
    {
        get
        {
            lock (_gate)
            {
                return [.. _sessions];
            }
        }
    }

    /// <inheritdoc />
    public Task AddAsync(UserSession session, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _sessions.Add(session);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<UserSession?> FindAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_sessions.FirstOrDefault(s => s.Id == sessionId));
        }
    }

    /// <inheritdoc />
    public async Task<(UserSession Session, Role Role)?> FindWithRoleAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await FindAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return null;
        }

        if (users is null)
        {
            return (session, Role.User);
        }

        var user = await users.FindByIdAsync(session.UserId, cancellationToken).ConfigureAwait(false);
        return user is null ? null : (session, user.Role);
    }

    /// <inheritdoc />
    public Task TouchAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _sessions.FirstOrDefault(s => s.Id == sessionId && s.RevokedAt is null)?.Touch(now);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<UserSession?> RotateAsync(string currentHash, string newHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var session = _sessions.FirstOrDefault(s => s.RefreshTokenHash == currentHash && s.IsActive(now));
            session?.Rotate(newHash, now);
            return Task.FromResult(session);
        }
    }

    /// <inheritdoc />
    public Task<int> RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var active = _sessions.Where(s => s.UserId == userId && s.RevokedAt is null).ToList();
            active.ForEach(s => s.Revoke(now));
            return Task.FromResult(active.Count);
        }
    }
}
