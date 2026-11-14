using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Geçerli fiyatı çözer: kullanıcının sürümlü <c>ModelPrice</c> kaydı (çağrı anında geçerli olan en yeni) varsa onu,
/// yoksa katalog fiyatını kullanır.
/// </summary>
public interface IModelPriceResolver
{
    /// <summary>Fiyatı çözer.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<ModelPricing> ResolveAsync(Guid userId, ModelInfo model, CancellationToken ct);
}
