using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Kullanım + fiyat → maliyet ($). Token, önbellek, uzun bağlam katmanı, web araması, görsel/karakter/dakika birimleri;
/// sağlayıcı maliyeti bildiriyorsa (OpenRouter) onu kullanır.
/// </summary>
public interface ICostCalculator
{
    /// <summary>Maliyeti hesaplar (6 ondalık).</summary>
    /// <param name="pricing">Fiyat.</param>
    /// <param name="usage">Kullanım.</param>
    decimal Calculate(ModelPricing pricing, CallUsage usage);
}
