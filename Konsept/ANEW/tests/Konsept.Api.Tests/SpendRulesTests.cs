using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Rules;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Harcama ve çağrı kuralları (spec 0013 AC-6, AC-9, AC-14, AC-15), saf: İstanbul ay sınırı (UTC kayıtlar), kesir kaybetmeyen
/// maliyet, token tahmini, yeniden deneme kuralı ve model yetkisi eşleşmesi.
/// </summary>
public sealed class SpendRulesTests
{
    /// <summary>
    /// AC-15: ayın son günü 23:59:59 (İstanbul, UTC 20:59:59) o aya; ertesi ayın ilk anı (İstanbul 00:00:00, UTC 21:00:00) yeni aya
    /// sayılır. Ay başlangıcı dahil, bitişi hariç; yıl dönümü de doğru.
    /// </summary>
    [Fact]
    public void IstanbulMonth_Boundaries()
    {
        var lastSecond = new DateTimeOffset(2026, 11, 30, 20, 59, 59, TimeSpan.Zero);
        var firstInstant = new DateTimeOffset(2026, 11, 30, 21, 0, 0, TimeSpan.Zero);

        var november = IstanbulMonth.Of(lastSecond);
        var december = IstanbulMonth.Of(firstInstant);

        november.StartUtc.ShouldBe(new DateTimeOffset(2026, 10, 31, 21, 0, 0, TimeSpan.Zero));
        november.EndUtc.ShouldBe(firstInstant);
        (lastSecond >= november.StartUtc && lastSecond < november.EndUtc).ShouldBeTrue();
        (firstInstant >= november.StartUtc && firstInstant < november.EndUtc).ShouldBeFalse();
        december.StartUtc.ShouldBe(firstInstant);
        december.EndUtc.ShouldBe(new DateTimeOffset(2026, 12, 31, 21, 0, 0, TimeSpan.Zero));
        IstanbulMonth.Of(new DateTimeOffset(2026, 12, 31, 21, 0, 0, TimeSpan.Zero)).StartUtc.ShouldBe(new DateTimeOffset(2026, 12, 31, 21, 0, 0, TimeSpan.Zero));
    }

    /// <summary>AC-9: maliyet yalnızca decimal ile, yuvarlamadan; tek token ve sıfır sınırları; görsel adet × fiyat.</summary>
    [Fact]
    public void SpendCost_ExactDecimal_Boundaries()
    {
        var text = new ModelEntry { Id = "x/y", ProviderId = "x", Name = "Y", InputPricePerMillionUsd = 0.25m, OutputPricePerMillionUsd = 2.00m };
        var image = new ModelEntry { Id = "x/i", ProviderId = "x", Name = "I", PricePerImageUsd = 0.042m };

        SpendCost.Text(text, 1, 0).ShouldBe(0.00000025m);
        SpendCost.Text(text, 0, 1).ShouldBe(0.000002m);
        SpendCost.Text(text, 0, 0).ShouldBe(0m);
        SpendCost.Text(text, 1234, 567).ShouldBe(0.0003085m + 0.001134m);
        SpendCost.Text(text, int.MaxValue, int.MaxValue).ShouldBe((int.MaxValue * 0.25m / 1_000_000m) + (int.MaxValue * 2.00m / 1_000_000m));
        SpendCost.Image(image, 3).ShouldBe(0.126m);
        SpendCost.EstimateTokens(null).ShouldBe(1);
        SpendCost.EstimateTokens(string.Empty).ShouldBe(1);
        SpendCost.EstimateTokens("abcd").ShouldBe(1);
        SpendCost.EstimateTokens("abcde").ShouldBe(2);
    }

    /// <summary>AC-14: toplama yuvarlanmamış değerlerle yapılır (1,234567 + 1,131 = 2,365567; tek tek yuvarlansaydı 2,36 olurdu).</summary>
    [Fact]
    public void SpendRules_SumAndRounding()
    {
        var sum = 1.234567m + 1.131m;

        sum.ShouldBe(2.365567m);
        decimal.Round(sum, 2, MidpointRounding.AwayFromZero).ShouldBe(2.37m);
        decimal.Round(2.365m, 2, MidpointRounding.AwayFromZero).ShouldBe(2.37m);
        (decimal.Round(1.234567m, 2) + decimal.Round(1.131m, 2)).ShouldBe(2.36m);
    }

    /// <summary>AC-6: yalnızca 429 ve 5xx tekrar edilir; en fazla 3 deneme; beklemeler 1 sn, 2 sn.</summary>
    [Fact]
    public void RetryRule_OnlyRetryable_IncreasingDelay()
    {
        RetryRule.MaxAttempts.ShouldBe(3);
        foreach (var status in new[] { 429, 500, 502, 503, 504, 599 })
        {
            RetryRule.IsRetryableStatus(status).ShouldBeTrue(status.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        foreach (var status in new[] { 400, 401, 402, 403, 404, 408, 409, 422, 428, 430, 499, 600, 200 })
        {
            RetryRule.IsRetryableStatus(status).ShouldBeFalse(status.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        RetryRule.DelayAfter(1).ShouldBe(TimeSpan.FromSeconds(1));
        RetryRule.DelayAfter(2).ShouldBe(TimeSpan.FromSeconds(2));
    }

    /// <summary>AC-2: yetki eşleşmesi ordinal ve tam; joker yalnızca <c>model.use:*</c>.</summary>
    [Fact]
    public void ModelPermission_ExactOrAll()
    {
        ModelPermission.Allows(["model.use:*"], "a/b").ShouldBeTrue();
        ModelPermission.Allows(["model.use:a/b"], "a/b").ShouldBeTrue();
        ModelPermission.Allows(["model.use:a/*"], "a/b").ShouldBeFalse();
        ModelPermission.Allows(["model.use:A/B"], "a/b").ShouldBeFalse();
        ModelPermission.Allows(["admin.panel"], "a/b").ShouldBeFalse();
        ModelPermission.Allows([], "a/b").ShouldBeFalse();
        ModelPermission.Allows(null, "a/b").ShouldBeFalse();
    }
}
