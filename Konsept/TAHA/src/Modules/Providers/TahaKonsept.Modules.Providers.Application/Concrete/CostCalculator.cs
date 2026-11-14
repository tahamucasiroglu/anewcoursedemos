using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Base;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Concrete;

/// <summary>
/// <see cref="ICostCalculator"/> uygulaması. Kural: sağlayıcı maliyet bildiriyorsa ve fiyat "sağlayıcı bildirir" ise ya da
/// token fiyatı yoksa bildirilen maliyet; aksi halde token (uzun bağlam katmanı dahil) + önbellek okuma/yazma
/// (yoksa girişin 0.1 / 1.25 katı) + web araması + görsel + bin karakter + dakika toplamı. 6 ondalığa yuvarlar.
/// </summary>
public sealed class CostCalculator : ProvidersServiceBase, ICostCalculator
{
    /// <summary>Milyon.</summary>
    private const decimal Million = 1_000_000m;

    /// <summary>Kurucu.</summary>
    /// <param name="clock">Saat.</param>
    public CostCalculator(IClock clock)
        : base(clock)
    {
    }

    /// <inheritdoc />
    public decimal Calculate(ModelPricing pricing, CallUsage usage)
    {
        ArgumentNullException.ThrowIfNull(pricing);
        ArgumentNullException.ThrowIfNull(usage);
        var hasTokenPrices = pricing.InputPerMTok is not null || pricing.OutputPerMTok is not null;
        if (usage.ProviderCostUsd is { } reported && (pricing.ProviderReportsCost || !hasTokenPrices))
        {
            return decimal.Round(Math.Max(0, reported), 6);
        }

        var longContext = pricing.LongContextThresholdTokens is { } threshold && usage.TotalInputTokens > threshold;
        var input = (longContext ? pricing.LongContextInputPerMTok ?? pricing.InputPerMTok : pricing.InputPerMTok) ?? 0m;
        var output = (longContext ? pricing.LongContextOutputPerMTok ?? pricing.OutputPerMTok : pricing.OutputPerMTok) ?? 0m;
        var cacheRead = pricing.CacheReadPerMTok ?? input * 0.1m;
        var cacheWrite = pricing.CacheWritePerMTok ?? input * 1.25m;

        var cost = usage.InputTokens * input / Million
            + usage.OutputTokens * output / Million
            + usage.CacheReadTokens * cacheRead / Million
            + usage.CacheWriteTokens * cacheWrite / Million
            + usage.WebSearchRequests * (pricing.WebSearchPer1K ?? 0m) / 1000m
            + usage.Images * (pricing.PerImage ?? 0m)
            + usage.Characters * (pricing.PerKChars ?? 0m) / 1000m
            + usage.AudioSeconds * (pricing.PerMinute ?? 0m) / 60m;
        return decimal.Round(cost, 6);
    }
}
