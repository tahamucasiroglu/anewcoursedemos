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

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation };
}
