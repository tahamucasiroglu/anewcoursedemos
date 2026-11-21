using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Billing.Plans;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Features.Billing.Payments;

/// <summary>
/// Tahsilat, iptali ve makbuz (spec 0015 gereksinim 5–7). Yetki <see cref="BillingGuard"/>: kurucu ya da planın şubesinde Muhasebe
/// (403 / 404). Kayıt plan + okul makbuz sırasında (<c>billing-plan:&lt;plan&gt;</c>, <c>billing-receipt:&lt;okul&gt;</c>; her zaman aynı
/// sırayla): plan ve taksitler sıra içinde taze okunur — aynı plana aynı anda gelen iki tahsilat kalan borcu aşamaz (biri "Tutar
/// kalan borçtan fazla olamaz."), sayaç tahsilatla aynı işlemde artar — makbuz numarası boşluksuz ve tekrarsız (okul + numara tekil
/// dizini ikinci katman). Tutar vadesi en eski ödenmemiş taksitten dağıtılır (<see cref="PaymentAllocator"/>). İptal plan sırasında:
/// dağılım geri alınır, kayıt silinmez, numara yeniden verilmez. Audit ayrıntısında plan, öğrenci ve makbuz no; tutar, not ve
/// gerekçe yok (loglarda da yok — yalnızca kimlik).
/// </summary>
public sealed class PaymentHandlers :
    IRequestHandler<RecordPaymentCommand, PaymentRecorded>,
    IRequestHandler<CancelPaymentCommand, BillingSaved>,
    IRequestHandler<GetReceiptQuery, Receipt>
{
    private readonly BillingGuard guard;
    private readonly IBillingRepository billing;
    private readonly ITenantRepository tenants;
    private readonly SchoolCalendar calendar;
    private readonly AdminAudit audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<PaymentHandlers> logger;

    public PaymentHandlers(
        BillingGuard guard,
        IBillingRepository billing,
        ITenantRepository tenants,
        SchoolCalendar calendar,
        AdminAudit audit,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<PaymentHandlers> logger)
    {
        this.guard = guard;
        this.billing = billing;
        this.tenants = tenants;
        this.calendar = calendar;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public static string ReceiptKey(Guid tenantId) => "billing-receipt:" + tenantId.ToString("N");

    public async Task<PaymentRecorded> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireStaffAsync(cancellationToken).ConfigureAwait(false);
        InstallmentPlan found = await this.billing.FindPlanAsync(request.PlanId, false, cancellationToken).ConfigureAwait(false)
            ?? throw await this.guard.NotFoundAsync(BillingMessages.PlanNotFound).ConfigureAwait(false);
        await this.guard.EnsureBranchAsync(staff, found.BranchId, BillingMessages.PlanNotFound).ConfigureAwait(false);
        _ = RecordPaymentCommandValidator.TryParseMethod(request.Method, out PaymentMethod method);
        decimal amount = BillingRules.Parse(request.Amount);

        IExclusiveScope scope = await this.guard.LockAsync([PlanHandlers.PlanKey(found.Id), ReceiptKey(staff.TenantId)], cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            InstallmentPlan plan = await this.billing.FindPlanAsync(found.Id, true, cancellationToken).ConfigureAwait(false)
                ?? throw await this.guard.NotFoundAsync(BillingMessages.PlanNotFound).ConfigureAwait(false);
            if (plan.Status != InstallmentPlanStatus.Active)
            {
                throw await this.guard.RuleAsync(BillingMessages.PlanCancelled).ConfigureAwait(false);
            }

            IReadOnlyList<Installment> installments = await this.billing.ListInstallmentsAsync([plan.Id], true, cancellationToken).ConfigureAwait(false);
            if (amount > PaymentAllocator.Remaining(installments))
            {
                throw await this.guard.RuleAsync(BillingMessages.AmountExceedsRemaining).ConfigureAwait(false);
            }

            DateTimeOffset now = this.timeProvider.GetUtcNow();
            ReceiptCounter counter = await this.billing.CounterForUpdateAsync(staff.TenantId, cancellationToken).ConfigureAwait(false);
            Payment payment = Payment.Record(plan, amount, request.Date!.Value, method, request.Note, counter.Next(), this.calendar.Today(), now, staff.UserId);
            IReadOnlyList<AllocationLine> lines = PaymentAllocator.Allocate(installments, amount);
            PaymentAllocator.Apply(installments, lines);
            this.billing.AddPayment(payment, [.. lines.Select(l => PaymentAllocation.Create(payment, l.InstallmentId, l.Amount, now))]);
            this.audit.Record(AuditAction.PaymentRecorded, staff.TenantId, staff.UserId, nameof(Payment), payment.Id, Detail(payment));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation(
                "Tahsilat kaydedildi: okul {TenantId}, öğrenci {StudentId}, plan {PlanId}, tahsilat {PaymentId}, makbuz {ReceiptNo}, taksit {AllocationCount}, kişi {UserId}",
                staff.TenantId,
                payment.StudentId,
                plan.Id,
                payment.Id,
                payment.ReceiptNo,
                lines.Count,
                staff.UserId);
            return new PaymentRecorded(payment.Id, payment.ReceiptNo);
        }
    }

    public async Task<BillingSaved> Handle(CancelPaymentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireStaffAsync(cancellationToken).ConfigureAwait(false);
        Payment found = await this.billing.FindPaymentAsync(request.PaymentId, false, cancellationToken).ConfigureAwait(false)
            ?? throw await this.guard.NotFoundAsync(BillingMessages.PaymentNotFound).ConfigureAwait(false);
        await this.guard.EnsureBranchAsync(staff, found.BranchId, BillingMessages.PaymentNotFound).ConfigureAwait(false);

        IExclusiveScope scope = await this.guard.LockAsync([PlanHandlers.PlanKey(found.PlanId)], cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Payment payment = await this.billing.FindPaymentAsync(found.Id, true, cancellationToken).ConfigureAwait(false)
                ?? throw await this.guard.NotFoundAsync(BillingMessages.PaymentNotFound).ConfigureAwait(false);
            if (payment.IsCancelled)
            {
                throw await this.guard.RuleAsync(BillingMessages.PaymentAlreadyCancelled).ConfigureAwait(false);
            }

            IReadOnlyList<PaymentAllocation> allocations = await this.billing.ListAllocationsAsync([payment.Id], cancellationToken).ConfigureAwait(false);
            IReadOnlyList<Installment> installments = await this.billing.ListInstallmentsAsync([payment.PlanId], true, cancellationToken).ConfigureAwait(false);
            PaymentAllocator.Revert(installments, allocations);
            payment.Cancel(request.Reason!, this.timeProvider.GetUtcNow(), staff.UserId);
            this.audit.Record(AuditAction.PaymentCancelled, staff.TenantId, staff.UserId, nameof(Payment), payment.Id, Detail(payment));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation(
                "Tahsilat iptal edildi: okul {TenantId}, öğrenci {StudentId}, plan {PlanId}, tahsilat {PaymentId}, makbuz {ReceiptNo}, kişi {UserId}",
                staff.TenantId,
                payment.StudentId,
                payment.PlanId,
                payment.Id,
                payment.ReceiptNo,
                staff.UserId);
            return new BillingSaved(payment.Id);
        }
    }

    public async Task<Receipt> Handle(GetReceiptQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireStaffAsync(cancellationToken).ConfigureAwait(false);
        Payment payment = await this.billing.FindPaymentAsync(request.PaymentId, false, cancellationToken).ConfigureAwait(false)
            ?? throw await this.guard.NotFoundAsync(BillingMessages.PaymentNotFound).ConfigureAwait(false);
        await this.guard.EnsureBranchAsync(staff, payment.BranchId, BillingMessages.PaymentNotFound).ConfigureAwait(false);
        StudentLabel student = await this.guard.RequireStudentAsync(staff, payment.StudentId, cancellationToken).ConfigureAwait(false);
        InstallmentPlan plan = await this.billing.FindPlanAsync(payment.PlanId, false, cancellationToken).ConfigureAwait(false)
            ?? throw await this.guard.NotFoundAsync(BillingMessages.PaymentNotFound).ConfigureAwait(false);
        Dictionary<Guid, Installment> installments = (await this.billing.ListInstallmentsAsync([plan.Id], false, cancellationToken).ConfigureAwait(false)).ToDictionary(i => i.Id);
        IReadOnlyList<PaymentAllocation> allocations = await this.billing.ListAllocationsAsync([payment.Id], cancellationToken).ConfigureAwait(false);
        string school = (await this.tenants.GetCurrentAsync(cancellationToken).ConfigureAwait(false))?.Name ?? string.Empty;
        string recorder = await this.billing.UserNameAsync(payment.RecordedBy, cancellationToken).ConfigureAwait(false);
        return new Receipt(
            payment.Id,
            payment.ReceiptNo,
            school,
            student.BranchName,
            payment.Date,
            student.FullName,
            plan.FeeName,
            payment.Amount,
            payment.Method.ToString(),
            [.. allocations.Select(a => installments[a.InstallmentId]).OrderBy(i => i.Sequence)
                .Select(i => new ReceiptAllocation(i.Sequence, i.DueDate, allocations.First(a => a.InstallmentId == i.Id).Amount))],
            recorder,
            payment.RecordedAt,
            payment.IsCancelled,
            payment.CancelledAt,
            payment.CancelReason);
    }

    private static string Detail(Payment payment) =>
        string.Create(CultureInfo.InvariantCulture, $"plan={payment.PlanId:D};student={payment.StudentId:D};receipt={payment.ReceiptNo}");
}
