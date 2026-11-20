using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Değişiklikleri tek işlemde yazar; EF'in eşzamanlılık hatasını uygulama katmanının
/// <see cref="ConcurrencyConflictException"/>'ına çevirir. <c>DiscardChanges</c> izlenen tüm kayıtları bırakır.
/// </summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly OkulDbContext context;

    public EfUnitOfWork(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await this.context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("Kayıt başka bir istek tarafından değiştirildi.", exception);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new ConcurrencyConflictException("Aynı kayıt başka bir istek tarafından eklendi.", exception);
        }
    }

    public void DiscardChanges() => this.context.ChangeTracker.Clear();

    public Task<IExclusiveScope> BeginExclusiveAsync(string key, CancellationToken cancellationToken) =>
        this.BeginExclusiveAsync([key], cancellationToken);

    public async Task<IExclusiveScope> BeginExclusiveAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(keys);
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await this.context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // İşlem süresince tutulan PostgreSQL kilidi; işlem bitince (commit/rollback) kendiliğinden bırakılır. Sıra
            // beklemesi en fazla 5 sn (yalnız bu işlem için); aşılırsa istek reddedilir, bağlantı süresiz tutulmaz.
            await this.context.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'", cancellationToken).ConfigureAwait(false);
            foreach (string key in keys.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
            {
                await this.context.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken).ConfigureAwait(false);
            }

            return new EfExclusiveScope(transaction);
        }
        catch (Npgsql.PostgresException exception) when (exception.SqlState == Npgsql.PostgresErrorCodes.LockNotAvailable)
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
            throw new ExclusiveLockTimeoutException("Aynı anahtar için sıra 5 sn içinde alınamadı.", exception);
        }
        catch
        {
            await transaction.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation };
}
