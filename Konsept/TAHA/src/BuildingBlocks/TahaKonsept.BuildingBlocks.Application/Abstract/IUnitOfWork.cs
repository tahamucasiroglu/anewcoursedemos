namespace TahaKonsept.BuildingBlocks.Application.Abstract;

/// <summary>
/// Değişiklikleri tek işlemde kaydetme sözleşmesi. Her modülün DbContext'i (<c>ModuleDbContextBase</c>) bunu uygular;
/// servisler buna <c>IRepository&lt;T&gt;.UnitOfWork</c> üzerinden erişir.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Bekleyen değişiklikleri kaydeder.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Etkilenen kayıt sayısı.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
