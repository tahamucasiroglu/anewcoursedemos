using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Features.Billing.Payments;
using OkulYonetim.Application.Features.Billing.Reminders;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Tahsilat kayıtları (spec 0015): bütün okumalar kurum/şube filtresinden geçer (filtre delme yok). Liste okumaları izlemesiz (T7);
/// yazılacak kayıtlar (<c>forUpdate</c>) izlenir ve çağıranın sırası (kilit) içinde okunur. Kaydeden kişinin adı kişi tablosundan
/// alt sorguyla (0013 <c>FindWithCreatorAsync</c> kalıbı).
/// </summary>
public sealed class BillingRepository : IBillingRepository
{
    private readonly OkulDbContext context;

    public BillingRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<FeeSchedule>> ListFeeSchedulesAsync(CancellationToken cancellationToken) =>
        await this.context.FeeSchedules.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<FeeSchedule?> FindFeeScheduleAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.FeeSchedules.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public void AddFeeSchedule(FeeSchedule fee) => this.context.FeeSchedules.Add(fee);

    public async Task<IReadOnlyList<DiscountDefinition>> ListDiscountsAsync(CancellationToken cancellationToken) =>
        await this.context.DiscountDefinitions.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<DiscountDefinition?> FindDiscountAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.DiscountDefinitions.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public void AddDiscount(DiscountDefinition discount) => this.context.DiscountDefinitions.Add(discount);

    public Task<InstallmentPlan?> FindPlanAsync(Guid id, bool forUpdate, CancellationToken cancellationToken) =>
        (forUpdate ? this.context.InstallmentPlans : this.context.InstallmentPlans.AsNoTracking()).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> HasActivePlanAsync(Guid studentId, Guid feeScheduleId, CancellationToken cancellationToken) =>
        this.context.InstallmentPlans.AnyAsync(p => p.StudentId == studentId && p.FeeScheduleId == feeScheduleId && p.Status == InstallmentPlanStatus.Active, cancellationToken);

    public Task<bool> HasValidPaymentAsync(Guid planId, CancellationToken cancellationToken) =>
        this.context.Payments.AnyAsync(p => p.PlanId == planId && p.CancelledAt == null, cancellationToken);

    public async Task<IReadOnlyList<InstallmentPlan>> ListPlansAsync(IReadOnlyCollection<Guid>? studentIds, IReadOnlyCollection<Guid>? branchIds, CancellationToken cancellationToken)
    {
        IQueryable<InstallmentPlan> query = this.context.InstallmentPlans.AsNoTracking();
        if (studentIds is not null)
        {
            query = query.Where(p => studentIds.Contains(p.StudentId));
        }

        if (branchIds is not null)
        {
            query = query.Where(p => branchIds.Contains(p.BranchId));
        }

        return await query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Installment>> ListInstallmentsAsync(IReadOnlyCollection<Guid> planIds, bool forUpdate, CancellationToken cancellationToken) =>
        await (forUpdate ? this.context.Installments : this.context.Installments.AsNoTracking())
            .Where(i => planIds.Contains(i.PlanId))
            .OrderBy(i => i.PlanId).ThenBy(i => i.Sequence)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void AddPlan(InstallmentPlan plan, IEnumerable<Installment> installments)
    {
        this.context.InstallmentPlans.Add(plan);
        this.context.Installments.AddRange(installments);
    }

    public Task<Payment?> FindPaymentAsync(Guid id, bool forUpdate, CancellationToken cancellationToken) =>
        (forUpdate ? this.context.Payments : this.context.Payments.AsNoTracking()).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PaymentEntry>> ListPaymentsAsync(IReadOnlyCollection<Guid> planIds, CancellationToken cancellationToken) =>
        [.. (await this.context.Payments.AsNoTracking()
            .Where(p => planIds.Contains(p.PlanId))
            .OrderBy(p => p.Date).ThenBy(p => p.ReceiptNo)
            .Select(p => new { Payment = p, Name = this.context.Users.Where(u => u.Id == p.RecordedBy).Select(u => u.FullName).FirstOrDefault() })
            .ToListAsync(cancellationToken).ConfigureAwait(false))
            .Select(x => new PaymentEntry(x.Payment, x.Name ?? string.Empty))];

    public async Task<string> UserNameAsync(Guid userId, CancellationToken cancellationToken) =>
        await this.context.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;

    public async Task<IReadOnlyList<PaymentAllocation>> ListAllocationsAsync(IReadOnlyCollection<Guid> paymentIds, CancellationToken cancellationToken) =>
        await this.context.PaymentAllocations.AsNoTracking()
            .Where(a => paymentIds.Contains(a.PaymentId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<ReceiptCounter> CounterForUpdateAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        ReceiptCounter? counter = await this.context.ReceiptCounters.FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken).ConfigureAwait(false);
        if (counter is null)
        {
            counter = ReceiptCounter.Start(tenantId);
            this.context.ReceiptCounters.Add(counter);
        }

        return counter;
    }

    public void AddPayment(Payment payment, IEnumerable<PaymentAllocation> allocations)
    {
        this.context.Payments.Add(payment);
        this.context.PaymentAllocations.AddRange(allocations);
    }

    public async Task<IReadOnlyList<ReminderCandidate>> ListReminderCandidatesAsync(DateOnly today, CancellationToken cancellationToken) =>
        [.. (await (from i in this.context.Installments.AsNoTracking()
                    join p in this.context.InstallmentPlans on i.PlanId equals p.Id
                    where p.Status == InstallmentPlanStatus.Active && i.DueDate < today && i.PaidAmount < i.Amount
                    orderby i.DueDate, i.Id
                    select new
                    {
                        Installment = i,
                        p.StudentId,
                        Last = this.context.PaymentReminders.Where(r => r.InstallmentId == i.Id).Max(r => (int?)r.Sequence) ?? 0,
                    })
                .ToListAsync(cancellationToken).ConfigureAwait(false))
            .Select(x => new ReminderCandidate(x.Installment, x.StudentId, x.Last))];

    public void AddReminder(PaymentReminder reminder) => this.context.PaymentReminders.Add(reminder);
}
