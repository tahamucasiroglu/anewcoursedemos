using Konsept.Modules.Production.Application.Abstract;
using Konsept.Modules.Production.Domain.Entities;
using Konsept.Modules.Production.Domain.Enums;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>
/// Üretim işleri deposu (spec 0006): her yazım koşullu tek ifade (durum koşulu yazımın içinde); kullanıcı başına tek çalışan iş kısmi tekil
/// dizinle zorlanır — yarışı kaybeden başlatma <c>23505</c> alır ve <c>false</c> döner.
/// </summary>
public sealed class ProductionJobRepository : EfRepositoryBase<ProductionDbContext>, IProductionJobRepository
{
    private static readonly ProductionJobStatus[] Running = [ProductionJobStatus.Producing, ProductionJobStatus.WaitingForAgent];

    /// <summary>Initializes a new instance of the <see cref="ProductionJobRepository"/> class.</summary>
    /// <param name="context">Bağlam.</param>
    public ProductionJobRepository(ProductionDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductionJob>> ListActiveAsync(Guid userId, CancellationToken cancellationToken) =>
        await Context.Jobs.AsNoTracking()
            .Where(j => j.UserId == userId && j.Status != ProductionJobStatus.Completed)
            .OrderBy(j => j.Position)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<ProductionJob?> GetAsync(Guid userId, Guid jobId, CancellationToken cancellationToken) =>
        Context.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId && j.UserId == userId, cancellationToken);

    /// <inheritdoc />
    public async Task<long> MaxPositionAsync(Guid userId, Guid profileId, CancellationToken cancellationToken) =>
        await Context.Jobs.Where(j => j.UserId == userId && j.ProfileId == profileId).MaxAsync(j => (long?)j.Position, cancellationToken).ConfigureAwait(false) ?? 0;

    /// <inheritdoc />
    public async Task AddAsync(ProductionJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        Context.Jobs.Add(job);
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Context.Entry(job).State = EntityState.Detached;
    }

    /// <inheritdoc />
    public async Task<bool> TryAddAsync(ProductionJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        Context.Jobs.Add(job);
        try
        {
            await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return false; // Konsept başına tek tamamlanmamış iş (spec 0020 Req 8: çift tıklama / yeniden gönderim).
        }
        finally
        {
            Context.Entry(job).State = EntityState.Detached;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SwapAsync(
        Guid userId, ProductionJob first, ProductionJob second, IReadOnlyCollection<ProductionJobStatus> movable, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var a = await Move(userId, first.Id, first.Position, second.Position, movable, cancellationToken).ConfigureAwait(false);
        var b = await Move(userId, second.Id, second.Position, first.Position, movable, cancellationToken).ConfigureAwait(false);
        if (a != 1 || b != 1)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(Guid userId, Guid jobId, IReadOnlyCollection<ProductionJobStatus> allowed, CancellationToken cancellationToken) =>
        await Context.Jobs
            .Where(j => j.Id == jobId && j.UserId == userId && allowed.Contains(j.Status))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false) == 1;

    /// <inheritdoc />
    public async Task<bool> StartAsync(
        Guid userId,
        Guid jobId,
        IReadOnlyCollection<ProductionJobStatus> from,
        FixedJobSettings fixedSettings,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        try
        {
            return await Context.Jobs
                .Where(j => j.Id == jobId && j.UserId == userId && from.Contains(j.Status))
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(j => j.Status, ProductionJobStatus.Producing)
                        .SetProperty(j => j.FixedSettings, fixedSettings)
                        .SetProperty(j => j.UpdatedAt, at),
                    cancellationToken)
                .ConfigureAwait(false) == 1;
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            return false; // Kullanıcının başka çalışan işi var (kısmi tekil dizin).
        }
    }

    /// <inheritdoc />
    public Task<int> StopAsync(Guid userId, Guid? profileId, DateTimeOffset at, CancellationToken cancellationToken) =>
        Context.Jobs
            .Where(j => j.UserId == userId && Running.Contains(j.Status) && (profileId == null || j.ProfileId == profileId))
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.Status, ProductionJobStatus.Stopped).SetProperty(j => j.UpdatedAt, at), cancellationToken);

    /// <inheritdoc />
    public Task SetProgressAsync(Guid userId, Guid jobId, JobProgress progress, DateTimeOffset at, CancellationToken cancellationToken) =>
        Context.Jobs
            .Where(j => j.Id == jobId && j.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.Progress, progress).SetProperty(j => j.UpdatedAt, at), cancellationToken);

    /// <inheritdoc />
    public async Task<bool> TransitionAsync(
        Guid userId, Guid jobId, IReadOnlyCollection<ProductionJobStatus> from, ProductionJobStatus to, DateTimeOffset at, CancellationToken cancellationToken)
    {
        try
        {
            return await Context.Jobs
                .Where(j => j.Id == jobId && j.UserId == userId && from.Contains(j.Status))
                .ExecuteUpdateAsync(s => s.SetProperty(j => j.Status, to).SetProperty(j => j.UpdatedAt, at), cancellationToken)
                .ConfigureAwait(false) == 1;
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            return false; // Tekil çalışan iş kısıtı.
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductionJob>> ListRunningAsync(CancellationToken cancellationToken) =>
        await Context.Jobs.AsNoTracking().Where(j => Running.Contains(j.Status)).ToListAsync(cancellationToken).ConfigureAwait(false);

    private static bool IsUniqueViolation(Exception ex) =>
        ex is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
        || ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private Task<int> Move(
        Guid userId, Guid jobId, long from, long to, IReadOnlyCollection<ProductionJobStatus> movable, CancellationToken cancellationToken) =>
        Context.Jobs
            .Where(j => j.Id == jobId && j.UserId == userId && j.Position == from && movable.Contains(j.Status))
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.Position, to), cancellationToken);
}
