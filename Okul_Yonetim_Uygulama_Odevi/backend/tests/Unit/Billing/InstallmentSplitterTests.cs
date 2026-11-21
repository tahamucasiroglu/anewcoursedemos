using OkulYonetim.Domain.Billing;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Billing;

/// <summary>
/// Spec 0015 AC-4 kuralları (birim): 10.000,00 + %15 → indirim 1.500,00, net 8.500,00, taksitler 2.833,33 / 2.833,33 / 2.833,34;
/// 1.000,00 / 7 taksitte kuruş farkı son taksitte; toplam her zaman neti tutar (1–12 taksit, tuhaf tutarlar); %100 indirimde net 0;
/// yüzde kuruşa yarım yukarı; 31 Ocak ilk vadede Şubat ayın son günü, Mart yine 31; taksit sayısı 0/13 ve ilk vade sınırları;
/// tarife/indirim sınır değerleri (10.000.000,00 kabul, 10.000.000,01 / 0 / 3 ondalık red; %1–%100).
/// </summary>
public sealed class InstallmentSplitterTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 20);

    [Fact]
    public void Open_TenThousandFifteenPercentThree_SplitsLikeSpec()
    {
        FeeSchedule fee = FeeSchedule.Create(Guid.NewGuid(), "Eğitim", 10_000.00m, Now, Guid.NewGuid());
        DiscountDefinition discount = DiscountDefinition.Create(fee.TenantId, "Kardeş", 15, Now, Guid.NewGuid());

        (InstallmentPlan plan, IReadOnlyList<Installment> installments) = InstallmentPlan.Open(fee.TenantId, Guid.NewGuid(), Guid.NewGuid(), fee, discount, 3, new DateOnly(2026, 12, 1), Today, Now, Guid.NewGuid());

        plan.DiscountAmount.ShouldBe(1_500.00m);
        plan.NetAmount.ShouldBe(8_500.00m);
        plan.FeeName.ShouldBe("Eğitim");
        plan.DiscountName.ShouldBe("Kardeş");
        plan.DiscountPercent.ShouldBe(15);
        installments.Select(i => i.Amount).ShouldBe([2_833.33m, 2_833.33m, 2_833.34m]);
        installments.Select(i => i.Sequence).ShouldBe([1, 2, 3]);
        installments.Select(i => i.DueDate).ShouldBe([new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 1), new DateOnly(2027, 2, 1)]);
        installments.Sum(i => i.Amount).ShouldBe(plan.NetAmount);
    }

    [Fact]
    public void Split_ThousandSeven_PennyDifferenceOnLast()
    {
        IReadOnlyList<InstallmentLine> lines = InstallmentSplitter.Split(1_000.00m, 7, new DateOnly(2027, 1, 10));

        lines.Take(6).ShouldAllBe(l => l.Amount == 142.85m);
        lines[6].Amount.ShouldBe(142.90m);
        lines.Sum(l => l.Amount).ShouldBe(1_000.00m);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("0.11")]
    [InlineData("1.00")]
    [InlineData("99.99")]
    [InlineData("8500.00")]
    [InlineData("12345.67")]
    [InlineData("9999999.99")]
    [InlineData("10000000.00")]
    public void Split_AnyCount_SumEqualsNetAndAllPositiveCents(string netText)
    {
        decimal net = decimal.Parse(netText, System.Globalization.CultureInfo.InvariantCulture);
        for (int count = 1; count <= 12; count++)
        {
            if (net < count * 0.01m)
            {
                continue;
            }

            IReadOnlyList<InstallmentLine> lines = InstallmentSplitter.Split(net, count, new DateOnly(2027, 1, 1));
            lines.Count.ShouldBe(count);
            lines.Sum(l => l.Amount).ShouldBe(net);
            lines.ShouldAllBe(l => l.Amount > 0m && decimal.Round(l.Amount, 2) == l.Amount);
            lines.Take(count - 1).Select(l => l.Amount).Distinct().Count().ShouldBeLessThanOrEqualTo(1);
            lines[^1].Amount.ShouldBeGreaterThanOrEqualTo(lines[0].Amount);
        }
    }

    [Fact]
    public void Split_JanuaryThirtyFirst_FebruaryLastDayThenBackToThirtyFirst()
    {
        IReadOnlyList<InstallmentLine> lines = InstallmentSplitter.Split(400m, 4, new DateOnly(2027, 1, 31));

        lines.Select(l => l.DueDate).ShouldBe([new DateOnly(2027, 1, 31), new DateOnly(2027, 2, 28), new DateOnly(2027, 3, 31), new DateOnly(2027, 4, 30)]);
        InstallmentSplitter.Split(200m, 2, new DateOnly(2028, 1, 31))[1].DueDate.ShouldBe(new DateOnly(2028, 2, 29));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Split_CountOutOfRange_Throws(int count)
    {
        InstallmentSplitter.IsValidCount(count).ShouldBeFalse();
        Should.Throw<ArgumentOutOfRangeException>(() => InstallmentSplitter.Split(100m, count, new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void NetOf_HundredPercent_NetZero_SplitRefuses()
    {
        InstallmentPlan.NetOf(10_000m, 100).ShouldBe((10_000m, 0m));
        Should.Throw<ArgumentOutOfRangeException>(() => InstallmentSplitter.Split(0m, 3, new DateOnly(2027, 1, 1)));
    }

    [Theory]
    [InlineData("100.05", 50, "50.03")]
    [InlineData("333.33", 10, "33.33")]
    [InlineData("0.05", 50, "0.03")]
    [InlineData("10000000.00", 1, "100000.00")]
    public void DiscountOf_RoundsHalfUpToCent(string fee, int percent, string expected)
    {
        decimal amount = decimal.Parse(fee, System.Globalization.CultureInfo.InvariantCulture);
        MoneyRules.DiscountOf(amount, percent).ShouldBe(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FirstDue_YesterdayAndDay366_Rejected_TodayAndDay365_Allowed()
    {
        InstallmentPlan.IsFirstDueAllowed(Today, Today).ShouldBeTrue();
        InstallmentPlan.IsFirstDueAllowed(Today.AddDays(365), Today).ShouldBeTrue();
        InstallmentPlan.IsFirstDueAllowed(Today.AddDays(-1), Today).ShouldBeFalse();
        InstallmentPlan.IsFirstDueAllowed(Today.AddDays(366), Today).ShouldBeFalse();
    }

    [Theory]
    [InlineData("10000000.00", true)]
    [InlineData("0.01", true)]
    [InlineData("10000000.01", false)]
    [InlineData("0", false)]
    [InlineData("-5", false)]
    [InlineData("1.005", false)]
    public void FeeAmount_Bounds(string amount, bool valid) =>
        FeeSchedule.IsValidAmount(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)).ShouldBe(valid);

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(101, false)]
    public void DiscountPercent_Bounds(int percent, bool valid) => DiscountDefinition.IsValidPercent(percent).ShouldBe(valid);

    [Fact]
    public void FeeUpdate_DoesNotChangeOpenedPlan()
    {
        FeeSchedule fee = FeeSchedule.Create(Guid.NewGuid(), "Eğitim", 9_000m, Now, Guid.NewGuid());
        (InstallmentPlan plan, _) = InstallmentPlan.Open(fee.TenantId, Guid.NewGuid(), Guid.NewGuid(), fee, null, 9, Today, Today, Now, Guid.NewGuid());

        fee.Update("Eğitim 2", 12_000m, Now, Guid.NewGuid());

        plan.FeeAmount.ShouldBe(9_000m);
        plan.FeeName.ShouldBe("Eğitim");
        plan.NetAmount.ShouldBe(9_000m);
    }
}
