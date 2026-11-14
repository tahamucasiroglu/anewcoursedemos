using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Concrete;

/// <summary>
/// <see cref="IModelPriceResolver"/> uygulaması: kullanıcının o anda geçerli <c>ModelPrice</c> kaydındaki dolu alanlar katalog
/// fiyatının üzerine yazılır; kayıt yoksa katalog fiyatı döner.
/// </summary>
public sealed class ModelPriceResolver : ProvidersServiceBase, IModelPriceResolver
{
    private readonly IModelPriceRepository _prices;

    /// <summary>Kurucu.</summary>
    /// <param name="prices">Fiyat deposu.</param>
    /// <param name="clock">Saat.</param>
    public ModelPriceResolver(IModelPriceRepository prices, IClock clock)
        : base(clock)
    {
        _prices = prices;
    }

    /// <inheritdoc />
    public async Task<ModelPricing> ResolveAsync(Guid userId, ModelInfo model, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(model);
        var overridePrice = await _prices.FindEffectiveAsync(userId, model.Key, Clock.UtcNow, ct);
        if (overridePrice is null)
        {
            return model.Pricing;
        }

        var p = model.Pricing;
        return p with
        {
            InputPerMTok = overridePrice.InputPerMTok ?? p.InputPerMTok,
            OutputPerMTok = overridePrice.OutputPerMTok ?? p.OutputPerMTok,
            CacheReadPerMTok = overridePrice.CacheReadPerMTok ?? p.CacheReadPerMTok,
            CacheWritePerMTok = overridePrice.CacheWritePerMTok ?? p.CacheWritePerMTok,
            PerImage = overridePrice.PerImage ?? p.PerImage,
            PerKChars = overridePrice.PerKChars ?? p.PerKChars,
            PerMinute = overridePrice.PerMinute ?? p.PerMinute,
        };
    }
}
