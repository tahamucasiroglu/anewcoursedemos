using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Identity.Infrastructure.Persistence.Concrete;

/// <summary>Ban kayıtlarının EF Core uygulaması (spec 0024 Req 8).</summary>
public sealed class UserBanRepository : EfRepositoryBase<IdentityModuleDbContext>, IUserBanRepository
{
    /// <summary>Initializes a new instance of the <see cref="UserBanRepository"/> class.</summary>
    /// <param name="context">Identity veritabanı bağlamı.</param>
    public UserBanRepository(IdentityModuleDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task AddAsync(UserBanRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        var entry = Context.UserBans.Add(record);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        entry.State = EntityState.Detached;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserBanRecord>> ListForUsersAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        await Context.UserBans
            .AsNoTracking()
            .Where(b => userIds.Contains(b.UserId))
            .OrderByDescending(b => b.At)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
