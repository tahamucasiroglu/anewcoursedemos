using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Paging;
using TahaKonsept.BuildingBlocks.Domain.Abstract;

namespace TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;

/// <summary>
/// <see cref="IRepository{TEntity}"/> için EF Core tabanı. Modül repository'si bundan türer:
/// <c>public sealed class ConceptRepository(ConceptsDbContext db) : Repository&lt;Concept&gt;(db), IConceptRepository;</c>
/// Önemli üyeler: <see cref="Context"/>, <see cref="Set"/>, <see cref="GetPagedAsync"/>.
/// </summary>
/// <typeparam name="TEntity">Aggregate kök türü.</typeparam>
public abstract class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class, IEntity<Guid>, IAggregateRoot
{
    /// <summary>Oluşturur.</summary>
    /// <param name="context">Modül DbContext'i.</param>
    protected Repository(ModuleDbContextBase context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Set = context.Set<TEntity>();
    }

    /// <summary>Modül DbContext'i.</summary>
    protected ModuleDbContextBase Context { get; }

    /// <summary>Varlık kümesi.</summary>
    protected DbSet<TEntity> Set { get; }

    /// <summary>İş birimi (DbContext).</summary>
    public IUnitOfWork UnitOfWork => Context;

    /// <summary>Takipli sorgu.</summary>
    /// <returns>Sorgu.</returns>
    public virtual IQueryable<TEntity> Query() => Set;

    /// <summary>Takipsiz sorgu.</summary>
    /// <returns>Sorgu.</returns>
    public virtual IQueryable<TEntity> QueryNoTracking() => Set.AsNoTracking();

    /// <summary>Kimliğe göre getirir.</summary>
    /// <param name="id">Kimlik.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Varlık ya da <c>null</c>.</returns>
    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    /// <summary>Koşula uyan ilk kaydı getirir (takipli).</summary>
    /// <param name="predicate">Koşul.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Varlık ya da <c>null</c>.</returns>
    public virtual Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(predicate, cancellationToken);

    /// <summary>Koşula uyanları listeler (takipsiz).</summary>
    /// <param name="predicate">Koşul.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Liste.</returns>
    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = QueryNoTracking();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>Kayıt var mı.</summary>
    /// <param name="predicate">Koşul.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Varsa <c>true</c>.</returns>
    public virtual Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(predicate, cancellationToken);

    /// <summary>Kayıt sayısı.</summary>
    /// <param name="predicate">Koşul.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayı.</returns>
    public virtual Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        predicate is null ? Set.CountAsync(cancellationToken) : Set.CountAsync(predicate, cancellationToken);

    /// <summary>Sayfalı liste (takipsiz). Sıralama verilmezse <c>Id</c> azalan (v7 → en yeni önce).</summary>
    /// <param name="request">Sayfa isteği.</param>
    /// <param name="predicate">Koşul.</param>
    /// <param name="orderBy">Sıralama.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
        PagedRequest request,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var query = QueryNoTracking();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        var total = await query.CountAsync(cancellationToken);
        var ordered = orderBy is not null
            ? orderBy(query)
            : query.OrderByDescending(e => EF.Property<Guid>(e, nameof(IEntity<Guid>.Id)));
        var items = await ordered.Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }

    /// <summary>Ekler.</summary>
    /// <param name="entity">Varlık.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    /// <summary>Güncellendi olarak işaretler.</summary>
    /// <param name="entity">Varlık.</param>
    public virtual void Update(TEntity entity) => Set.Update(entity);

    /// <summary>Siler.</summary>
    /// <param name="entity">Varlık.</param>
    public virtual void Remove(TEntity entity) => Set.Remove(entity);
}
