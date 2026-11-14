namespace TahaKonsept.BuildingBlocks.Domain.Abstract;

/// <summary>
/// Oluşturulma / güncellenme zamanını (UTC) taşıyan varlık sözleşmesi.
/// Değerler <c>ModuleDbContextBase.SaveChangesAsync</c> içinde <c>IClock</c> ile otomatik doldurulur.
/// </summary>
public interface IAuditable
{
    /// <summary>Kaydın oluşturulduğu an (UTC).</summary>
    DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Kaydın son güncellendiği an (UTC).</summary>
    DateTimeOffset UpdatedAtUtc { get; set; }
}
