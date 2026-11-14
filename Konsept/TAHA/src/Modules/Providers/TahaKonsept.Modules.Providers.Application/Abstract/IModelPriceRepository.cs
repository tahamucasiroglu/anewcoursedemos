using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Providers.Domain.Entities;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Sürümlü fiyat deposu.
/// </summary>
public interface IModelPriceRepository : IRepository<ModelPrice>
{
    /// <summary>Verilen anda geçerli en yeni fiyatı bulur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="modelKey">Katalog anahtarı.</param>
    /// <param name="atUtc">An.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<ModelPrice?> FindEffectiveAsync(Guid userId, string modelKey, DateTimeOffset atUtc, CancellationToken ct);
}
