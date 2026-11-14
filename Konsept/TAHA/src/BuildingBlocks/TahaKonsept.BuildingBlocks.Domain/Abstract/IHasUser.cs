namespace TahaKonsept.BuildingBlocks.Domain.Abstract;

/// <summary>
/// Bir kullanıcıya ait olan varlık sözleşmesi. Tek kullanıcı modunda da tüm entity'ler (Identity hariç)
/// <see cref="UserId"/> taşır; ileride çok kullanıcıya geçişte veri ayrımı bununla yapılır.
/// </summary>
public interface IHasUser
{
    /// <summary>Kaydın sahibi olan kullanıcının kimliği.</summary>
    Guid UserId { get; }
}
