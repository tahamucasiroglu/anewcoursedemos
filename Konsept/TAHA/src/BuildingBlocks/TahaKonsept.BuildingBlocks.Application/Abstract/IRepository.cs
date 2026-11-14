using System.Linq.Expressions;
using TahaKonsept.BuildingBlocks.Application.Paging;
using TahaKonsept.BuildingBlocks.Domain.Abstract;

namespace TahaKonsept.BuildingBlocks.Application.Abstract;

/// <summary>
/// Aggregate kökleri için genel repository sözleşmesi (ekle, getir, sorgula, sayfala, sil).
/// Değişiklikler <see cref="UnitOfWork"/> ile kaydedilir. Somut taban: <c>TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base.Repository&lt;TEntity&gt;</c>.
/// </summary>
/// <typeparam name="TEntity">Varlık türü (Guid kimlikli aggregate kökü).</typeparam>
public interface IRepository<TEntity>
    where TEntity : class, IEntity<Guid>, IAggregateRoot
{
    /// <summary>Bu repository'nin bağlı olduğu iş birimi (modül DbContext'i).</summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>Takip edilen (değiştirilebilir) sorgu kökü.</summary>
    /// <returns>Sorgu.</returns>
    IQueryable<TEntity> Query();

    /// <summary>Takip edilmeyen (salt okunur) sorgu kökü.</summary>
    /// <returns>Sorgu.</returns>
    IQueryable<TEntity> QueryNoTracking();

    /// <summary>Kimliğe göre getirir.</summary>
    /// <param name="id">Kimlik.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Varlık ya da <c>null</c>.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Koşula uyan ilk kaydı getirir.</summary>
    /// <param name="predicate">Koşul.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Varlık ya da <c>null</c>.</returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Koşula uyan kayıtları listeler (takipsiz).</summary>
    /// <param name="predicate">Koşul (<c>null</c> ise hepsi).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Liste.</returns>
    Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>Koşula uyan kayıt var mı.</summary>
    /// <param name="predicate">Koşul.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Varsa <c>true</c>.</returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Koşula uyan kayıt sayısı.</summary>
    /// <param name="predicate">Koşul (<c>null</c> ise hepsi).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayı.</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>Sayfalı liste (takipsiz).</summary>
    /// <param name="request">Sayfa isteği.</param>
    /// <param name="predicate">Koşul.</param>
    /// <param name="orderBy">Sıralama (verilmezse <c>Id</c> azalan; v7 Guid → en yeni önce).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    Task<PagedResult<TEntity>> GetPagedAsync(
        PagedRequest request,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>Yeni varlık ekler (kaydetmek için <see cref="UnitOfWork"/>).</summary>
    /// <param name="entity">Varlık.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Varlığı güncellendi olarak işaretler.</summary>
    /// <param name="entity">Varlık.</param>
    void Update(TEntity entity);

    /// <summary>Varlığı siler.</summary>
    /// <param name="entity">Varlık.</param>
    void Remove(TEntity entity);
}
