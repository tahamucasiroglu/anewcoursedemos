using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Features.Billing.Tariffs;

/// <summary>
/// Ücret tarifeleri (spec 0015 gereksinim 1). Liste kurucu ve muhasebeye (plan formu), yazma yalnızca kurucuya (<see cref="BillingGuard"/>;
/// rol her istekte veritabanından). Ekleme, düzenleme ve etkinleştirme okulun katalog sırasında (<c>billing-catalog:&lt;okul&gt;</c>):
/// etkinler arasında ad tekilliği (Türkçe harf kuralı, kırpılmış) aynı anda gelen iki istekle bozulamaz. Her değişiklik audit'te
/// (ayrıntısız — ad ve tutar yazılmaz); loglarda yalnızca kimlik.
/// </summary>
public sealed class FeeScheduleHandlers :
    IRequestHandler<ListFeeSchedulesQuery, IReadOnlyList<FeeScheduleRow>>,
    IRequestHandler<CreateFeeScheduleCommand, FeeScheduleRow>,
    IRequestHandler<UpdateFeeScheduleCommand, FeeScheduleRow>,
    IRequestHandler<SetFeeScheduleActiveCommand, FeeScheduleRow>
{
    private static readonly StringComparer Turkish = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);

    private readonly BillingGuard guard;
    private readonly IBillingRepository billing;
    private readonly AdminAudit audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<FeeScheduleHandlers> logger;

    public FeeScheduleHandlers(BillingGuard guard, IBillingRepository billing, AdminAudit audit, IUnitOfWork unitOfWork, TimeProvider timeProvider, ILogger<FeeScheduleHandlers> logger)
    {
        this.guard = guard;
        this.billing = billing;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<FeeScheduleRow>> Handle(ListFeeSchedulesQuery request, CancellationToken cancellationToken)
    {
        await this.guard.RequireStaffAsync(cancellationToken).ConfigureAwait(false);
        return [.. (await this.billing.ListFeeSchedulesAsync(cancellationToken).ConfigureAwait(false))
            .OrderByDescending(f => f.IsActive).ThenBy(f => f.Name, Turkish).ThenBy(f => f.Id)
            .Select(Row)];
    }

    public async Task<FeeScheduleRow> Handle(CreateFeeScheduleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireFounderAsync(cancellationToken).ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync([CatalogKey(staff.TenantId)], cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            await this.EnsureNameFreeAsync(request.Name!, null, cancellationToken).ConfigureAwait(false);
            FeeSchedule fee = FeeSchedule.Create(staff.TenantId, request.Name!, BillingRules.Parse(request.Amount), this.timeProvider.GetUtcNow(), staff.UserId);
            this.billing.AddFeeSchedule(fee);
            this.audit.Record(AuditAction.FeeScheduleCreated, staff.TenantId, staff.UserId, nameof(FeeSchedule), fee.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Tarife eklendi: okul {TenantId}, tarife {FeeScheduleId}, kişi {UserId}", staff.TenantId, fee.Id, staff.UserId);
            return Row(fee);
        }
    }

    public async Task<FeeScheduleRow> Handle(UpdateFeeScheduleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireFounderAsync(cancellationToken).ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync([CatalogKey(staff.TenantId)], cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            FeeSchedule fee = await this.billing.FindFeeScheduleAsync(request.Id, cancellationToken).ConfigureAwait(false)
                ?? throw await this.guard.NotFoundAsync(BillingMessages.FeeScheduleNotFound).ConfigureAwait(false);
            if (fee.IsActive)
            {
                await this.EnsureNameFreeAsync(request.Name!, fee.Id, cancellationToken).ConfigureAwait(false);
            }

            fee.Update(request.Name!, BillingRules.Parse(request.Amount), this.timeProvider.GetUtcNow(), staff.UserId);
            this.audit.Record(AuditAction.FeeScheduleUpdated, staff.TenantId, staff.UserId, nameof(FeeSchedule), fee.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Tarife düzenlendi: okul {TenantId}, tarife {FeeScheduleId}, kişi {UserId}", staff.TenantId, fee.Id, staff.UserId);
            return Row(fee);
        }
    }

    public async Task<FeeScheduleRow> Handle(SetFeeScheduleActiveCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireFounderAsync(cancellationToken).ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync([CatalogKey(staff.TenantId)], cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            FeeSchedule fee = await this.billing.FindFeeScheduleAsync(request.Id, cancellationToken).ConfigureAwait(false)
                ?? throw await this.guard.NotFoundAsync(BillingMessages.FeeScheduleNotFound).ConfigureAwait(false);
            if (request.Active && !fee.IsActive)
            {
                await this.EnsureNameFreeAsync(fee.Name, fee.Id, cancellationToken).ConfigureAwait(false);
            }

            if (fee.SetActive(request.Active, this.timeProvider.GetUtcNow(), staff.UserId))
            {
                this.audit.Record(request.Active ? AuditAction.FeeScheduleActivated : AuditAction.FeeScheduleDeactivated, staff.TenantId, staff.UserId, nameof(FeeSchedule), fee.Id);
                await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
                this.logger.LogInformation("Tarife {State}: okul {TenantId}, tarife {FeeScheduleId}, kişi {UserId}", request.Active ? "etkin" : "pasif", staff.TenantId, fee.Id, staff.UserId);
            }

            return Row(fee);
        }
    }

    public static string CatalogKey(Guid tenantId) => "billing-catalog:" + tenantId.ToString("N");

    private static FeeScheduleRow Row(FeeSchedule fee) => new(fee.Id, fee.Name, fee.Amount, fee.IsActive);

    private async Task EnsureNameFreeAsync(string name, Guid? self, CancellationToken cancellationToken)
    {
        string clean = name.Trim();
        if ((await this.billing.ListFeeSchedulesAsync(cancellationToken).ConfigureAwait(false))
            .Any(f => f.IsActive && f.Id != self && Turkish.Equals(f.Name.Trim(), clean)))
        {
            throw await this.guard.RuleAsync(BillingMessages.FeeNameTaken).ConfigureAwait(false);
        }
    }
}
