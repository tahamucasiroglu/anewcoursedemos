using OkulYonetim.Application.Features.Billing.Payments;
using OkulYonetim.Application.Features.Billing.Reminders;
using OkulYonetim.Domain.Billing;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Tahsilat ve ön muhasebe kayıtları (spec 0015): tarife, indirim, plan + taksit, tahsilat + dağılım, makbuz sayacı ve hatırlatma.
/// Hepsi kurum (plan/tahsilat ayrıca şube) filtresinden geçer; filtre delme yok. Okumalar varsayılan izlemesiz (T7); "forUpdate"
/// okumaları sıra (kilit) içinde yapılır ki kalan borç taze okunsun.
/// </summary>
public interface IBillingRepository
{
    Task<IReadOnlyList<FeeSchedule>> ListFeeSchedulesAsync(CancellationToken cancellationToken);

    Task<FeeSchedule?> FindFeeScheduleAsync(Guid id, CancellationToken cancellationToken);

    void AddFeeSchedule(FeeSchedule fee);

    Task<IReadOnlyList<DiscountDefinition>> ListDiscountsAsync(CancellationToken cancellationToken);

    Task<DiscountDefinition?> FindDiscountAsync(Guid id, CancellationToken cancellationToken);

    void AddDiscount(DiscountDefinition discount);

    Task<InstallmentPlan?> FindPlanAsync(Guid id, bool forUpdate, CancellationToken cancellationToken);

    Task<bool> HasActivePlanAsync(Guid studentId, Guid feeScheduleId, CancellationToken cancellationToken);

    Task<bool> HasValidPaymentAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>Planlar (iptal dahil); öğrenci ve/veya şube listesiyle süzülür (<c>null</c> = süzme yok).</summary>
    Task<IReadOnlyList<InstallmentPlan>> ListPlansAsync(IReadOnlyCollection<Guid>? studentIds, IReadOnlyCollection<Guid>? branchIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<Installment>> ListInstallmentsAsync(IReadOnlyCollection<Guid> planIds, bool forUpdate, CancellationToken cancellationToken);

    void AddPlan(InstallmentPlan plan, IEnumerable<Installment> installments);

    Task<Payment?> FindPaymentAsync(Guid id, bool forUpdate, CancellationToken cancellationToken);

    /// <summary>Planların tahsilatları (iptal dahil), kaydeden kişinin adıyla.</summary>
    Task<IReadOnlyList<PaymentEntry>> ListPaymentsAsync(IReadOnlyCollection<Guid> planIds, CancellationToken cancellationToken);

    Task<string> UserNameAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PaymentAllocation>> ListAllocationsAsync(IReadOnlyCollection<Guid> paymentIds, CancellationToken cancellationToken);

    /// <summary>Okulun makbuz sayacı (izlenen); yoksa oluşturulur ve eklenir. Okul sırası içinde çağrılır.</summary>
    Task<ReceiptCounter> CounterForUpdateAsync(Guid tenantId, CancellationToken cancellationToken);

    void AddPayment(Payment payment, IEnumerable<PaymentAllocation> allocations);

    /// <summary>Geçerli planların bugünden önce vadesi dolmuş ve kalanı olan taksitleri, gönderilmiş en büyük hatırlatma sırasıyla.</summary>
    Task<IReadOnlyList<ReminderCandidate>> ListReminderCandidatesAsync(DateOnly today, CancellationToken cancellationToken);

    void AddReminder(PaymentReminder reminder);
}
