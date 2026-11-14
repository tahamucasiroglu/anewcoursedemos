using TahaKonsept.BuildingBlocks.Domain.Abstract;

namespace TahaKonsept.BuildingBlocks.Domain.Base;

/// <summary>
/// Tüm varlıkların ortak tabanı: kimlik (<see cref="Id"/>) ve kimliğe dayalı eşitlik.
/// Guid kimlikli varlıklar kurucuda <c>Guid.CreateVersion7()</c> atar (ya da <see cref="AggregateRoot"/> türetir).
/// </summary>
/// <typeparam name="TId">Kimlik türü.</typeparam>
public abstract class EntityBase<TId> : IEntity<TId>
    where TId : notnull
{
    /// <summary>EF Core ve türetilmiş sınıflar için boş kurucu.</summary>
    protected EntityBase()
    {
        Id = default!;
    }

    /// <summary>Verilen kimlikle varlık oluşturur.</summary>
    /// <param name="id">Varlık kimliği.</param>
    protected EntityBase(TId id)
    {
        Id = id;
    }

    /// <summary>Varlığın benzersiz kimliği.</summary>
    public TId Id { get; protected set; }

    /// <summary>Aynı somut türde ve aynı kimlikte ise iki varlık eşittir.</summary>
    /// <param name="obj">Karşılaştırılacak nesne.</param>
    /// <returns>Eşitse <c>true</c>.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not EntityBase<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType()
            && !EqualityComparer<TId>.Default.Equals(Id, default!)
            && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <summary>Kimliğe dayalı hash kodu.</summary>
    /// <returns>Hash kodu.</returns>
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
