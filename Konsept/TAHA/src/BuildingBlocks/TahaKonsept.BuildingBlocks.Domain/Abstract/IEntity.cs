namespace TahaKonsept.BuildingBlocks.Domain.Abstract;

/// <summary>
/// Kimliği olan her domain varlığının sözleşmesi.
/// Önemli üye: <see cref="Id"/> (varlığın benzersiz kimliği).
/// </summary>
/// <typeparam name="TId">Kimlik türü (genellikle <see cref="Guid"/>, v7).</typeparam>
public interface IEntity<out TId>
    where TId : notnull
{
    /// <summary>Varlığın benzersiz kimliği.</summary>
    TId Id { get; }
}
