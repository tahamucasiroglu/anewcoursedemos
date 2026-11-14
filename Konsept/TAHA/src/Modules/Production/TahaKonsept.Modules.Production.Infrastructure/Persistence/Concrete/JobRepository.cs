using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IJobRepository"/>'nin EF Core uygulaması: işler her zaman aşama çalıştırmalarıyla birlikte yüklenir; ajan komut kimliğinden iş bulma.
/// </summary>
/// <param name="context">Modül DbContext'i.</param>
public sealed class JobRepository(ProductionDbContext context) : Repository<Job>(context), IJobRepository
{
    /// <summary>İşler (çalıştırmalarla, takipli).</summary>
    /// <returns>Sorgu.</returns>
    public override IQueryable<Job> Query() => base.Query().Include(x => x.Runs);

    /// <summary>İşler (çalıştırmalarla, takipsiz).</summary>
    /// <returns>Sorgu.</returns>
    public override IQueryable<Job> QueryNoTracking() => base.QueryNoTracking().Include(x => x.Runs);

    /// <summary>İş + çalıştırmalar (takipli).</summary>
    /// <param name="id">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İş ya da <c>null</c>.</returns>
    public Task<Job?> GetWithRunsAsync(Guid id, CancellationToken cancellationToken) =>
        Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <summary>Kullanıcının işi (takipsiz).</summary>
    /// <param name="id">İş.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İş ya da <c>null</c>.</returns>
    public Task<Job?> GetForUserNoTrackingAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        QueryNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

    /// <summary>Komut kimliğine göre iş (takipli).</summary>
    /// <param name="commandId">Komut.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İş ya da <c>null</c>.</returns>
    public async Task<Job?> FindByCommandAsync(Guid commandId, CancellationToken cancellationToken)
    {
        var jobId = await Context.Set<StageRun>().AsNoTracking().Where(r => r.CommandId == commandId).Select(r => (Guid?)r.JobId).FirstOrDefaultAsync(cancellationToken);
        return jobId is null ? null : await GetWithRunsAsync(jobId.Value, cancellationToken);
    }

    /// <summary>İşin çalıştırmaları (takipsiz).</summary>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Çalıştırmalar.</returns>
    public async Task<IReadOnlyList<StageRun>> ListRunsNoTrackingAsync(Guid jobId, CancellationToken cancellationToken) =>
        await Context.Set<StageRun>().AsNoTracking().Where(r => r.JobId == jobId).OrderBy(r => r.Sequence).ToListAsync(cancellationToken);

    /// <summary>Birden çok iş (takipsiz).</summary>
    /// <param name="ids">İşler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İşler.</returns>
    public async Task<IReadOnlyList<Job>> ListWithRunsNoTrackingAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await QueryNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
}
