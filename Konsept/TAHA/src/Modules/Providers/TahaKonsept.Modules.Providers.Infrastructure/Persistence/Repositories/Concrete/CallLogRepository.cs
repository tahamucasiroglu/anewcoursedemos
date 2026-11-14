using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Domain.Entities;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.Persistence.Repositories.Concrete;

/// <summary>
/// <see cref="ICallLogRepository"/> EF uygulaması (platforma göre toplam SQL'de gruplanır).
/// </summary>
public sealed class CallLogRepository : Repository<CallLog>, ICallLogRepository
{
    /// <summary>Kurucu.</summary>
    /// <param name="context">DbContext.</param>
    public CallLogRepository(ProvidersDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlatformUsage>> SumByPlatformAsync(Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        var from = fromUtc.ToUniversalTime();
        var to = toUtc.ToUniversalTime();
        return await Set.AsNoTracking()
            .Where(c => c.UserId == userId && c.CreatedAtUtc >= from && c.CreatedAtUtc < to)
            .GroupBy(c => c.Platform)
            .Select(g => new PlatformUsage { Platform = g.Key, CostUsd = g.Sum(c => c.CostUsd), Calls = g.Count() })
            .ToListAsync(ct);
    }
}
