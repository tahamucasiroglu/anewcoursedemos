using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>Sıra durumu deposu (spec 0017 Req 11): kullanıcı başına tek satır, tek ifadelik upsert.</summary>
public sealed class QueueStateRepository : EfRepositoryBase<ProductionDbContext>, IQueueStateRepository
{
    /// <summary>Initializes a new instance of the <see cref="QueueStateRepository"/> class.</summary>
    /// <param name="context">Bağlam.</param>
    public QueueStateRepository(ProductionDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public Task<QueueState?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        Context.QueueStates.AsNoTracking().FirstOrDefaultAsync(q => q.UserId == userId, cancellationToken);

    /// <inheritdoc />
    public Task SetAsync(QueueState state, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        return Context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO production.queue_states (user_id, consecutive_failures, halted, updated_at)
            VALUES ({state.UserId}, {state.ConsecutiveFailures}, {state.Halted}, {state.UpdatedAt})
            ON CONFLICT (user_id) DO UPDATE SET
                consecutive_failures = EXCLUDED.consecutive_failures, halted = EXCLUDED.halted, updated_at = EXCLUDED.updated_at
            """,
            cancellationToken);
    }
}
