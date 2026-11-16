using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.AiGateway.Infrastructure.Persistence.Concrete;

/// <summary>
/// Harcama kayıtlarının EF Core + PostgreSQL uygulaması. Toplam her zaman <c>user_id</c> ile süzülür (BR-1) ve veritabanında
/// yuvarlamadan (<c>numeric</c>) hesaplanır.
/// </summary>
public sealed class SpendRepository : EfRepositoryBase<AiGatewayDbContext>, ISpendRepository
{
    /// <summary>Initializes a new instance of the <see cref="SpendRepository"/> class.</summary>
    /// <param name="context">AiGateway veritabanı bağlamı.</param>
    public SpendRepository(AiGatewayDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task AddAsync(SpendRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        Context.SpendRecords.Add(record);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Context.Entry(record).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public async Task<decimal> SumAsync(Guid userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        await Context.SpendRecords.AsNoTracking()
            .Where(r => r.UserId == userId && r.OccurredAt >= fromUtc && r.OccurredAt < toUtc)
            .SumAsync(r => r.CostUsd, cancellationToken)
            .ConfigureAwait(false);
}
