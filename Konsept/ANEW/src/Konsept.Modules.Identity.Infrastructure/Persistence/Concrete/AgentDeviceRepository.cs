using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Identity.Infrastructure.Persistence.Concrete;

/// <summary>
/// Ajan cihazlarının EF Core uygulaması (spec 0012). Son kullanım ve iptal tek ifadelik <c>UPDATE</c>'lerdir; iptal edilen
/// cihazın belirteci bir sonraki bağlantı denemesinde reddedilir.
/// </summary>
public sealed class AgentDeviceRepository : EfRepositoryBase<IdentityModuleDbContext>, IAgentDeviceRepository
{
    /// <summary>Initializes a new instance of the <see cref="AgentDeviceRepository"/> class.</summary>
    /// <param name="context">Identity veritabanı bağlamı.</param>
    public AgentDeviceRepository(IdentityModuleDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task AddAsync(AgentDevice device, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(device);
        var entry = Context.AgentDevices.Add(device);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        entry.State = EntityState.Detached;
    }

    /// <inheritdoc />
    public Task<AgentDevice?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Context.AgentDevices.AsNoTracking().FirstOrDefaultAsync(d => d.TokenHash == tokenHash, cancellationToken);

    /// <inheritdoc />
    public Task TouchAsync(Guid deviceId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var utc = now.ToUniversalTime();
        return Context.AgentDevices
            .Where(d => d.Id == deviceId && d.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(d => d.LastUsedAt, utc), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> RevokeActiveAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var utc = now.ToUniversalTime();
        var ids = await Context.AgentDevices.AsNoTracking()
            .Where(d => d.UserId == userId && d.RevokedAt == null)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (ids.Count > 0)
        {
            await Context.AgentDevices
                .Where(d => ids.Contains(d.Id) && d.RevokedAt == null)
                .ExecuteUpdateAsync(set => set.SetProperty(d => d.RevokedAt, utc), cancellationToken)
                .ConfigureAwait(false);
        }

        return ids;
    }
}
