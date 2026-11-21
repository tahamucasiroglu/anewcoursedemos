using OkulYonetim.Domain.Billing;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Billing;

/// <summary>
/// Spec 0015 AC-6 ve AC-7 kuralları (birim): 3 × 1.000 TL planda 1.500 TL 1. taksidi Ödendi, 2.'yi Kısmen ödendi (500 kalan) yapar;
/// dağıtım vadesi en eski ödenmemiş taksitten başlar; kalan borca eşit tutar kabul, 0,01 fazlası red; iptal dağılımı geri alır ve
/// geçmiş vadeli taksit yeniden Gecikti olur. Durum: vade günü Bekliyor/Kısmen ödendi, ertesi gün Gecikti; kalan yoksa Ödendi.
/// </summary>
public sealed class PaymentAllocatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 20);

    [Fact]
    public void Allocate_FifteenHundredOnThreeThousands_FirstPaidSecondPartial()
    {
        (InstallmentPlan plan, IReadOnlyList<Installment> installments) = ThreeThousands(Today);

        IReadOnlyList<AllocationLine> lines = PaymentAllocator.Allocate(installments, 1_500m);
        PaymentAllocator.Apply(installments, lines);

        lines.Select(l => l.Amount).ShouldBe([1_000m, 500m]);
        installments[0].StateOn(Today).ShouldBe(InstallmentState.Paid);
        installments[1].StateOn(Today).ShouldBe(InstallmentState.PartiallyPaid);
        installments[1].Remaining.ShouldBe(500m);
        installments[2].StateOn(Today).ShouldBe(InstallmentState.Pending);
        PaymentAllocator.Remaining(installments).ShouldBe(1_500m);
        plan.NetAmount.ShouldBe(3_000m);
    }

    [Fact]
    public void Allocate_ExactRemainingAccepted_OneCentMoreRejected()
    {
        (_, IReadOnlyList<Installment> installments) = ThreeThousands(Today);
        PaymentAllocator.Apply(installments, PaymentAllocator.Allocate(installments, 1_234.56m));

        Should.Throw<ArgumentOutOfRangeException>(() => PaymentAllocator.Allocate(installments, 1_765.45m));
        IReadOnlyList<AllocationLine> rest = PaymentAllocator.Allocate(installments, 1_765.44m);
        PaymentAllocator.Apply(installments, rest);

        installments.ShouldAllBe(i => i.StateOn(Today) == InstallmentState.Paid);
        PaymentAllocator.Remaining(installments).ShouldBe(0m);
        Should.Throw<ArgumentOutOfRangeException>(() => PaymentAllocator.Allocate(installments, 0.01m));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.001")]
    public void Allocate_InvalidAmount_Rejected(string amount)
    {
        (_, IReadOnlyList<Installment> installments) = ThreeThousands(Today);
        Should.Throw<ArgumentOutOfRangeException>(() => PaymentAllocator.Allocate(installments, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Allocate_SkipsPaidAndStartsFromOldestDue()
    {
        (_, IReadOnlyList<Installment> installments) = ThreeThousands(Today);
        installments[0].Apply(1_000m);

        IReadOnlyList<AllocationLine> lines = PaymentAllocator.Allocate(installments, 1_200m);

        lines.ShouldBe([new AllocationLine(installments[1].Id, 1_000m), new AllocationLine(installments[2].Id, 200m)]);
    }

    [Fact]
    public void Revert_RestoresBalances_PastDueBecomesOverdueAgain()
    {
        DateOnly firstDue = Today;
        (InstallmentPlan plan, IReadOnlyList<Installment> installments) = ThreeThousands(firstDue);
        Payment payment = Payment.Record(plan, 1_500m, Today, PaymentMethod.Cash, null, 1, Today, Now, Guid.NewGuid());
        List<PaymentAllocation> allocations = [.. PaymentAllocator.Allocate(installments, payment.Amount).Select(l => PaymentAllocation.Create(payment, l.InstallmentId, l.Amount, Now))];
        PaymentAllocator.Apply(installments, allocations.Select(a => new AllocationLine(a.InstallmentId, a.Amount)));
        DateOnly later = firstDue.AddDays(40);
        installments[0].StateOn(later).ShouldBe(InstallmentState.Paid);

        payment.Cancel("Yanlış öğrenciye girildi", Now, Guid.NewGuid());
        PaymentAllocator.Revert(installments, allocations);

        installments.Select(i => i.PaidAmount).ShouldBe([0m, 0m, 0m]);
        installments[0].StateOn(later).ShouldBe(InstallmentState.Overdue);
        installments[0].OverdueOn(later).ShouldBe(1_000m);
        payment.IsCancelled.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => payment.Cancel("ikinci kez", Now, Guid.NewGuid()));
    }

    [Fact]
    public void StateOn_DueDayNotOverdue_NextDayOverdueEvenIfPartial()
    {
        (_, IReadOnlyList<Installment> installments) = ThreeThousands(Today);
        installments[0].StateOn(Today).ShouldBe(InstallmentState.Pending);
        installments[0].StateOn(Today.AddDays(1)).ShouldBe(InstallmentState.Overdue);
        installments[0].Apply(400m);
        installments[0].StateOn(Today).ShouldBe(InstallmentState.PartiallyPaid);
        installments[0].StateOn(Today.AddDays(1)).ShouldBe(InstallmentState.Overdue);
        installments[0].OverdueOn(Today.AddDays(1)).ShouldBe(600m);
        installments[0].OverdueOn(Today).ShouldBe(0m);
    }

    [Fact]
    public void Payment_RulesOnDateNoteAndCancelledPlan()
    {
        (InstallmentPlan plan, _) = ThreeThousands(Today);
        Payment.IsDateAllowed(Today, Today).ShouldBeTrue();
        Payment.IsDateAllowed(Today.AddDays(-365), Today).ShouldBeTrue();
        Payment.IsDateAllowed(Today.AddDays(-366), Today).ShouldBeFalse();
        Payment.IsDateAllowed(Today.AddDays(1), Today).ShouldBeFalse();
        Should.Throw<ArgumentOutOfRangeException>(() => Payment.Record(plan, 10m, Today, PaymentMethod.Cash, new string('n', 201), 1, Today, Now, Guid.NewGuid()));
        Payment.Record(plan, 10m, Today, PaymentMethod.Cash, new string('n', 200), 1, Today, Now, Guid.NewGuid()).Note!.Length.ShouldBe(200);
        Should.Throw<ArgumentOutOfRangeException>(() => Payment.Record(plan, 10m, Today, (PaymentMethod)9, null, 1, Today, Now, Guid.NewGuid()));

        plan.Cancel(Now, Guid.NewGuid());
        Should.Throw<InvalidOperationException>(() => Payment.Record(plan, 10m, Today, PaymentMethod.Cash, null, 2, Today, Now, Guid.NewGuid()));
    }

    [Fact]
    public void ReceiptCounter_StartsAtOneAndIncrements()
    {
        ReceiptCounter counter = ReceiptCounter.Start(Guid.NewGuid());
        counter.Next().ShouldBe(1);
        counter.Next().ShouldBe(2);
        counter.LastNumber.ShouldBe(2);
    }

    private static (InstallmentPlan Plan, IReadOnlyList<Installment> Installments) ThreeThousands(DateOnly firstDue)
    {
        FeeSchedule fee = FeeSchedule.Create(Guid.NewGuid(), "Eğitim", 3_000m, Now, Guid.NewGuid());
        return InstallmentPlan.Open(fee.TenantId, Guid.NewGuid(), Guid.NewGuid(), fee, null, 3, firstDue, firstDue, Now, Guid.NewGuid());
    }
}
