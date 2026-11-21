using OkulYonetim.Application.Features.Billing.Payments;
using OkulYonetim.Application.Features.Billing.Reminders;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Tests.Integration.Administration;

namespace OkulYonetim.Tests.Integration.Billing;

/// <summary>
/// Gerçek tahsilat deposunun sarmalayıcısı (spec 0015 AC-12): tahsilatın sıra içindeki taze okumaları kapıdan geçer — taksitlerin
/// "forUpdate" okuması <c>installments</c> kapısından (kalan borç), makbuz sayacı <c>counter</c> kapısından. Sıra varsa ikinci istek
/// kapıya giremez (<see cref="RaceGate.SecondEnteredWhileFirstWaited"/> = false); sıra kaldırılırsa girer ve iki istek aynı değeri okur.
/// Diğer işlemler olduğu gibi aktarılır.
/// </summary>
public sealed class GatedBillingRepository : IBillingRepository
{
    private readonly IBillingRepository inner;
    private readonly RaceGate installments;
    private readonly RaceGate counter;

    public GatedBillingRepository(IBillingRepository inner, RaceGate installments, RaceGate counter)
    {
        this.inner = inner;
        this.installments = installments;
        this.counter = counter;
    }

    public async Task<IReadOnlyList<Installment>> ListInstallmentsAsync(IReadOnlyCollection<Guid> planIds, bool forUpdate, CancellationToken cancellationToken)
    {
        if (forUpdate)
        {
            await this.installments.PassAsync().ConfigureAwait(false);
        }

        return await this.inner.ListInstallmentsAsync(planIds, forUpdate, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReceiptCounter> CounterForUpdateAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await this.counter.PassAsync().ConfigureAwait(false);
        return await this.inner.CounterForUpdateAsync(tenantId, cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<FeeSchedule>> ListFeeSchedulesAsync(CancellationToken cancellationToken) => this.inner.ListFeeSchedulesAsync(cancellationToken);

    public Task<FeeSchedule?> FindFeeScheduleAsync(Guid id, CancellationToken cancellationToken) => this.inner.FindFeeScheduleAsync(id, cancellationToken);

    public void AddFeeSchedule(FeeSchedule fee) => this.inner.AddFeeSchedule(fee);

    public Task<IReadOnlyList<DiscountDefinition>> ListDiscountsAsync(CancellationToken cancellationToken) => this.inner.ListDiscountsAsync(cancellationToken);

    public Task<DiscountDefinition?> FindDiscountAsync(Guid id, CancellationToken cancellationToken) => this.inner.FindDiscountAsync(id, cancellationToken);

    public void AddDiscount(DiscountDefinition discount) => this.inner.AddDiscount(discount);

    public Task<InstallmentPlan?> FindPlanAsync(Guid id, bool forUpdate, CancellationToken cancellationToken) => this.inner.FindPlanAsync(id, forUpdate, cancellationToken);

    public Task<bool> HasActivePlanAsync(Guid studentId, Guid feeScheduleId, CancellationToken cancellationToken) => this.inner.HasActivePlanAsync(studentId, feeScheduleId, cancellationToken);

    public Task<bool> HasValidPaymentAsync(Guid planId, CancellationToken cancellationToken) => this.inner.HasValidPaymentAsync(planId, cancellationToken);

    public Task<IReadOnlyList<InstallmentPlan>> ListPlansAsync(IReadOnlyCollection<Guid>? studentIds, IReadOnlyCollection<Guid>? branchIds, CancellationToken cancellationToken) =>
        this.inner.ListPlansAsync(studentIds, branchIds, cancellationToken);

    public void AddPlan(InstallmentPlan plan, IEnumerable<Installment> installments) => this.inner.AddPlan(plan, installments);

    public Task<Payment?> FindPaymentAsync(Guid id, bool forUpdate, CancellationToken cancellationToken) => this.inner.FindPaymentAsync(id, forUpdate, cancellationToken);

    public Task<IReadOnlyList<PaymentEntry>> ListPaymentsAsync(IReadOnlyCollection<Guid> planIds, CancellationToken cancellationToken) => this.inner.ListPaymentsAsync(planIds, cancellationToken);

    public Task<string> UserNameAsync(Guid userId, CancellationToken cancellationToken) => this.inner.UserNameAsync(userId, cancellationToken);

    public Task<IReadOnlyList<PaymentAllocation>> ListAllocationsAsync(IReadOnlyCollection<Guid> paymentIds, CancellationToken cancellationToken) => this.inner.ListAllocationsAsync(paymentIds, cancellationToken);

    public void AddPayment(Payment payment, IEnumerable<PaymentAllocation> allocations) => this.inner.AddPayment(payment, allocations);

    public Task<IReadOnlyList<ReminderCandidate>> ListReminderCandidatesAsync(DateOnly today, CancellationToken cancellationToken) => this.inner.ListReminderCandidatesAsync(today, cancellationToken);

    public void AddReminder(PaymentReminder reminder) => this.inner.AddReminder(reminder);
}
