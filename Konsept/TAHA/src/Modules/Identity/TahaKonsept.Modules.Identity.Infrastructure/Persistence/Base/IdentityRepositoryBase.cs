using Microsoft.EntityFrameworkCore;

namespace TahaKonsept.Modules.Identity.Infrastructure.Persistence.Base;

/// <summary>
/// Identity depolarının tabanı (<see cref="IdentityModuleDbContext"/> üzerinde; ortak <c>Repository&lt;T&gt;</c>
/// <c>ModuleDbContextBase</c> istediği için kullanılamaz). <see cref="Set"/> izlenen, <see cref="QueryNoTracking"/> izlenmeyen sorgu.
/// </summary>
/// <typeparam name="TEntity">Varlık türü.</typeparam>
public abstract class IdentityRepositoryBase<TEntity>
    where TEntity : class
{
    /// <summary>Oluşturur.</summary>
    /// <param name="context">Identity DbContext.</param>
    protected IdentityRepositoryBase(IdentityModuleDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>DbContext.</summary>
    protected IdentityModuleDbContext Context { get; }

    /// <summary>İzlenen küme.</summary>
    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    /// <summary>İzlenmeyen sorgu.</summary>
    /// <returns>Sorgu.</returns>
    protected IQueryable<TEntity> QueryNoTracking() => Set.AsNoTracking();
}
