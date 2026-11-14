using TahaKonsept.BuildingBlocks.Domain.Abstract;

namespace TahaKonsept.BuildingBlocks.Domain.Base;

/// <summary>
/// Guid (v7) kimlikli aggregate kökü tabanı. Kurucu, kimliği <c>Guid.CreateVersion7()</c> ile otomatik üretir.
/// Repository'ler (<c>IRepository&lt;TEntity&gt;</c>) bu türden türeyen varlıklarla çalışır.
/// </summary>
public abstract class AggregateRoot : EntityBase<Guid>, IAggregateRoot
{
    /// <summary>Yeni bir v7 Guid kimliğiyle aggregate oluşturur.</summary>
    protected AggregateRoot()
        : base(Guid.CreateVersion7())
    {
    }

    /// <summary>Var olan bir kimlikle aggregate oluşturur.</summary>
    /// <param name="id">Kimlik.</param>
    protected AggregateRoot(Guid id)
        : base(id)
    {
    }
}
