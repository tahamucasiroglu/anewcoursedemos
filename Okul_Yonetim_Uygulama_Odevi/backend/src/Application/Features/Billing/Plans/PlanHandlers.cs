using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Features.Billing.Plans;

/// <summary>
/// Taksit planı: önizleme, açma, iptal (spec 0015 gereksinim 3–4). Yetki <see cref="BillingGuard"/>: kurucu ya da öğrencinin şubesinde
/// Muhasebe (değilse 403), öğrenci/plan görünmüyor ya da başka şubede 404. Öğrenci etkin olmalı; tarife ve (verildiyse) indirim bu
/// okulda olmalı (değilse 404) ve etkin olmalı. Net sıfırsa "Net tutar 0 TL; …". Açma öğrenci sırasında (<c>billing-student:&lt;öğrenci&gt;</c>):
/// aynı tarifeden geçerli ikinci plan aynı anda gelen iki istekle de açılamaz (veritabanındaki filtreli tekil dizin ikinci katman).
/// İptal plan sırasında (<c>billing-plan:&lt;plan&gt;</c>, tahsilatla aynı anahtar): geçerli tahsilat varsa reddedilir. Audit ayrıntısında
/// yalnızca kimlik ve taksit sayısı; tutar yok.
/// </summary>
public sealed class PlanHandlers :
    IRequestHandler<PreviewInstallmentPlanQuery, PlanPreview>,
    IRequestHandler<CreateInstallmentPlanCommand, BillingSaved>,
    IRequestHandler<CancelInstallmentPlanCommand, BillingSaved>
{
    private readonly BillingGuard guard;
    private readonly IBillingRepository billing;
    private readonly SchoolCalendar calendar;
    private readonly AdminAudit audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<PlanHandlers> logger;

    public PlanHandlers(BillingGuard guard, IBillingRepository billing, SchoolCalendar calendar, AdminAudit audit, IUnitOfWork unitOfWork, TimeProvider timeProvider, ILogger<PlanHandlers> logger)
    {
        this.guard = guard;
        this.billing = billing;
        this.calendar = calendar;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public static string PlanKey(Guid planId) => "billing-plan:" + planId.ToString("N");

    public async Task<PlanPreview> Handle(PreviewInstallmentPlanQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireStaffAsync(cancellationToken).ConfigureAwait(false);
        (StudentLabel student, FeeSchedule fee, DiscountDefinition? discount) = await this.PrepareAsync(staff, request.StudentId, request.FeeScheduleId, request.DiscountId, cancellationToken).ConfigureAwait(false);
        (InstallmentPlan plan, IReadOnlyList<Installment> installments) = InstallmentPlan.Open(
            staff.TenantId, student.BranchId, student.Id, fee, discount, request.InstallmentCount!.Value, request.FirstDueDate!.Value, this.calendar.Today(), this.timeProvider.GetUtcNow(), staff.UserId);
        return new PlanPreview(
            plan.FeeName,
            plan.FeeAmount,
            plan.DiscountName,
            plan.DiscountPercent,
            plan.DiscountAmount,
            plan.NetAmount,
            [.. installments.Select(i => new InstallmentLine(i.Sequence, i.DueDate, i.Amount))]);
    }

    public async Task<BillingSaved> Handle(CreateInstallmentPlanCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireStaffAsync(cancellationToken).ConfigureAwait(false);
        await this.guard.RequireStudentAsync(staff, request.StudentId, cancellationToken).ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(["billing-student:" + request.StudentId.ToString("N")], cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            (StudentLabel student, FeeSchedule fee, DiscountDefinition? discount) = await this.PrepareAsync(staff, request.StudentId, request.FeeScheduleId, request.DiscountId, cancellationToken).ConfigureAwait(false);
            if (await this.billing.HasActivePlanAsync(student.Id, fee.Id, cancellationToken).ConfigureAwait(false))
            {
                throw await this.guard.RuleAsync(BillingMessages.PlanExists).ConfigureAwait(false);
            }

            (InstallmentPlan plan, IReadOnlyList<Installment> installments) = InstallmentPlan.Open(
                staff.TenantId, student.BranchId, student.Id, fee, discount, request.InstallmentCount!.Value, request.FirstDueDate!.Value, this.calendar.Today(), this.timeProvider.GetUtcNow(), staff.UserId);
            this.billing.AddPlan(plan, installments);
            this.audit.Record(
                AuditAction.InstallmentPlanCreated,
                staff.TenantId,
                staff.UserId,
                nameof(InstallmentPlan),
                plan.Id,
                string.Create(CultureInfo.InvariantCulture, $"student={student.Id:D};installments={installments.Count}"));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation(
                "Taksit planı açıldı: okul {TenantId}, öğrenci {StudentId}, plan {PlanId}, taksit {Count}, kişi {UserId}",
                staff.TenantId,
                student.Id,
                plan.Id,
                installments.Count,
                staff.UserId);
            return new BillingSaved(plan.Id);
        }
    }

    public async Task<BillingSaved> Handle(CancelInstallmentPlanCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireStaffAsync(cancellationToken).ConfigureAwait(false);
        InstallmentPlan found = await this.billing.FindPlanAsync(request.PlanId, false, cancellationToken).ConfigureAwait(false)
            ?? throw await this.guard.NotFoundAsync(BillingMessages.PlanNotFound).ConfigureAwait(false);
        await this.guard.EnsureBranchAsync(staff, found.BranchId, BillingMessages.PlanNotFound).ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync([PlanKey(found.Id)], cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            InstallmentPlan plan = await this.billing.FindPlanAsync(found.Id, true, cancellationToken).ConfigureAwait(false)
                ?? throw await this.guard.NotFoundAsync(BillingMessages.PlanNotFound).ConfigureAwait(false);
            if (plan.Status == InstallmentPlanStatus.Cancelled)
            {
                throw await this.guard.RuleAsync(BillingMessages.PlanAlreadyCancelled).ConfigureAwait(false);
            }

            if (await this.billing.HasValidPaymentAsync(plan.Id, cancellationToken).ConfigureAwait(false))
            {
                throw await this.guard.RuleAsync(BillingMessages.PlanHasPayments).ConfigureAwait(false);
            }

            plan.Cancel(this.timeProvider.GetUtcNow(), staff.UserId);
            this.audit.Record(
                AuditAction.InstallmentPlanCancelled,
                staff.TenantId,
                staff.UserId,
                nameof(InstallmentPlan),
                plan.Id,
                string.Create(CultureInfo.InvariantCulture, $"student={plan.StudentId:D}"));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Taksit planı iptal edildi: okul {TenantId}, öğrenci {StudentId}, plan {PlanId}, kişi {UserId}", staff.TenantId, plan.StudentId, plan.Id, staff.UserId);
            return new BillingSaved(plan.Id);
        }
    }

    /// <summary>Öğrenci (görünür, şubesinde, etkin), tarife ve indirim (bu okulda, etkin) ve sıfırdan büyük net.</summary>
    private async Task<(StudentLabel Student, FeeSchedule Fee, DiscountDefinition? Discount)> PrepareAsync(BillingStaff staff, Guid studentId, Guid feeScheduleId, Guid? discountId, CancellationToken cancellationToken)
    {
        StudentLabel student = await this.guard.RequireStudentAsync(staff, studentId, cancellationToken).ConfigureAwait(false);
        if (!student.IsActive)
        {
            throw await this.guard.RuleAsync(BillingMessages.StudentLeft).ConfigureAwait(false);
        }

        FeeSchedule fee = await this.billing.FindFeeScheduleAsync(feeScheduleId, cancellationToken).ConfigureAwait(false)
            ?? throw await this.guard.NotFoundAsync(BillingMessages.FeeScheduleNotFound).ConfigureAwait(false);
        if (!fee.IsActive)
        {
            throw await this.guard.RuleAsync(BillingMessages.FeeScheduleInactive).ConfigureAwait(false);
        }

        DiscountDefinition? discount = null;
        if (discountId is { } id)
        {
            discount = await this.billing.FindDiscountAsync(id, cancellationToken).ConfigureAwait(false)
                ?? throw await this.guard.NotFoundAsync(BillingMessages.DiscountNotFound).ConfigureAwait(false);
            if (!discount.IsActive)
            {
                throw await this.guard.RuleAsync(BillingMessages.DiscountInactive).ConfigureAwait(false);
            }
        }

        if (InstallmentPlan.NetOf(fee.Amount, discount?.Percent).Net <= 0m)
        {
            throw await this.guard.RuleAsync(BillingMessages.NetZero).ConfigureAwait(false);
        }

        return (student, fee, discount);
    }
}
