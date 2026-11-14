namespace Konsept.Shared.Infrastructure.Persistence.Base;

/// <summary>
/// EF Core repository'lerinin ortak temeli: modülün kendi <c>DbContext</c>'ine erişim verir.
/// </summary>
/// <typeparam name="TContext">Modülün <see cref="ModuleDbContextBase"/> türevi.</typeparam>
public abstract class EfRepositoryBase<TContext>
    where TContext : ModuleDbContextBase
{
    /// <summary>Initializes a new instance of the <see cref="EfRepositoryBase{TContext}"/> class.</summary>
    /// <param name="context">Modülün veritabanı bağlamı.</param>
    protected EfRepositoryBase(TContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>Gets modülün veritabanı bağlamı.</summary>
    protected TContext Context { get; }
}
