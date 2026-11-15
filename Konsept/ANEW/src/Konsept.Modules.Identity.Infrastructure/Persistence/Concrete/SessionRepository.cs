using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Modules.Identity.Domain.Enums;
using Konsept.Modules.Identity.Domain.Rules;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Identity.Infrastructure.Persistence.Concrete;

/// <summary>
/// Oturumların EF Core uygulaması. Dönüş, son kullanım ve iptal tek ifadelik <c>UPDATE</c>'lerdir: aynı eski refresh token
/// ikinci kez dönemez (plan Risk 4), iptal edilen oturum bir sonraki istekte reddedilir (spec 0002 AC-14).
/// </summary>
public sealed class SessionRepository : EfRepositoryBase<IdentityModuleDbContext>, ISessionRepository
{
    /// <summary>Initializes a new instance of the <see cref="SessionRepository"/> class.</summary>
    /// <param name="context">Identity veritabanı bağlamı.</param>
    public SessionRepository(IdentityModuleDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task AddAsync(UserSession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        var entry = Context.Sessions.Add(session);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        entry.State = EntityState.Detached;
    }

    /// <inheritdoc />
    public Task<UserSession?> FindAsync(Guid sessionId, CancellationToken cancellationToken) =>
        Context.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

    /// <inheritdoc />
    public async Task<(UserSession Session, Role Role)?> FindWithRoleAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        // Oturum ve rol tek sorguda (aynı şema içinde JOIN; spec 0011 plan Risk 1): her Api isteğinde ek gidiş-dönüş yok.
        var row = await (
            from session in Context.Sessions.AsNoTracking()
            join user in Context.Users.AsNoTracking() on session.UserId equals user.Id
            where session.Id == sessionId
            select new { Session = session, user.Role })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return row is null ? null : (row.Session, row.Role);
    }

    /// <inheritdoc />
    public Task TouchAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var utc = now.ToUniversalTime();
        return Context.Sessions
            .Where(s => s.Id == sessionId && s.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.LastUsedAt, utc), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserSession?> RotateAsync(string currentHash, string newHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var utc = now.ToUniversalTime();
        var cutoff = utc - SessionPolicy.Lifetime;
        var updated = await Context.Sessions
            .Where(s => s.RefreshTokenHash == currentHash && s.RevokedAt == null && s.LastUsedAt >= cutoff)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.RefreshTokenHash, newHash).SetProperty(s => s.LastUsedAt, utc), cancellationToken)
            .ConfigureAwait(false);
        return updated == 1
            ? await Context.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.RefreshTokenHash == newHash, cancellationToken).ConfigureAwait(false)
            : null;
    }

    /// <inheritdoc />
    public Task<int> RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var utc = now.ToUniversalTime();
        return Context.Sessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.RevokedAt, utc), cancellationToken);
    }
}
