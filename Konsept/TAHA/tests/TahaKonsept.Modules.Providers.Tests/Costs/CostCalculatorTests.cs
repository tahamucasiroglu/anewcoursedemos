using Shouldly;
using TahaKonsept.Modules.Providers.Application.Concrete;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.Costs;

/// <summary>
/// Kullanım → maliyet: token, önbellek, uzun bağlam katmanı, web araması, birim fiyatlar, sağlayıcı maliyeti.
/// </summary>
public sealed class CostCalculatorTests
{
    private readonly CostCalculator _calculator = new(new FixedClock());

    /// <summary>Opus: giriş + çıkış + önbellek okuma/yazma + web araması.</summary>
    [Fact]
    public void Opus_tokens_cache_and_web_search()
    {
        var usage = new CallUsage { InputTokens = 10_000, OutputTokens = 2_000, CacheReadTokens = 50_000, CacheWriteTokens = 4_000, WebSearchRequests = 3 };
        // 10k*4 + 2k*20 + 50k*0.2 + 4k*5 = 0.04 + 0.04 + 0.01 + 0.02 = 0.11 ; 3 arama * $10/1000 = 0.03
        _calculator.Calculate(TestFixtures.Model("claude-opus-5-5").Pricing, usage).ShouldBe(0.14m);
    }

    /// <summary>Haiku 100K üstü uzun bağlam fiyatı.</summary>
    [Fact]
    public void Haiku_long_context_tier()
    {
        var pricing = TestFixtures.Model("claude-haiku-5-5").Pricing;
        _calculator.Calculate(pricing, new CallUsage { InputTokens = 50_000, OutputTokens = 1_000 }).ShouldBe(0.0055m);
        _calculator.Calculate(pricing, new CallUsage { InputTokens = 150_000, OutputTokens = 1_000 }).ShouldBe(0.0775m);
    }

    /// <summary>OpenRouter: sağlayıcının bildirdiği maliyet kullanılır.</summary>
    [Fact]
    public void Provider_reported_cost_wins_for_openrouter()
    {
        var usage = new CallUsage { InputTokens = 1_000_000, OutputTokens = 1_000_000, ProviderCostUsd = 0.0123m };
        _calculator.Calculate(TestFixtures.Model("openrouter").Pricing, usage).ShouldBe(0.0123m);
    }

    /// <summary>Görsel, bin karakter ve dakika birimleri.</summary>
    [Fact]
    public void Unit_prices()
    {
        _calculator.Calculate(TestFixtures.Model("gemini-nano-banana-2.1").Pricing, new CallUsage { Images = 8 }).ShouldBe(0.2688m);
        _calculator.Calculate(TestFixtures.Model("elevenlabs-v4-turbo").Pricing, new CallUsage { Characters = 2_500 }).ShouldBe(0.1m);
        _calculator.Calculate(new ModelPricing { PerMinute = 0.015m }, new CallUsage { AudioSeconds = 120 }).ShouldBe(0.03m);
    }

    /// <summary>Önbellek fiyatı yoksa girişin 0.1 katı varsayılır.</summary>
    [Fact]
    public void Missing_cache_price_defaults_to_tenth_of_input()
    {
        var usage = new CallUsage { InputTokens = 800, CacheReadTokens = 200, OutputTokens = 500 };
        _calculator.Calculate(TestFixtures.Model("gpt-6.1-sol").Pricing, usage).ShouldBe(0.00664m);
    }
}
