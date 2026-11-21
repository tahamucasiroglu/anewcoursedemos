using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Billing.Tariffs;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Billing;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Features.Billing.Discounts;

/// <summary>
/// İndirim/burs tanımları (spec 0015 gereksinim 2; tarifeyle aynı kurallar). Liste kurucu ve muhasebeye (plan formu), yazma yalnızca kurucuya (<see cref="BillingGuard"/>;
/// rol her istekte veritabanından). Ekleme, düzenleme ve etkinleştirme okulun katalog sırasında (<c>billing-catalog:&lt;okul&gt;</c>):
/// etkinler arasında ad tekilliği (Türkçe harf kuralı, kırpılmış) aynı anda gelen iki istekle bozulamaz. Her değişiklik audit'te
/// (ayrıntısız — ad ve tutar yazılmaz); loglarda yalnızca kimlik.
/// </summary>
public sealed class DiscountHandlers :
    IRequestHandler<ListDiscountsQuery, IReadOnlyList<DiscountRow>>,
    IRequestHandler<CreateDiscountCommand, DiscountRow>,
    IRequestHandler<UpdateDiscountCommand, DiscountRow>,
    IRequestHandler<SetDiscountActiveCommand, DiscountRow>
{
    private static readonly StringComparer Turkish = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);

    private readonly BillingGuard guard;
    private readonly IBillingRepository billing;
    private readonly AdminAudit audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<DiscountHandlers> logger;

    public DiscountHandlers(BillingGuard guard, IBillingRepository billing, AdminAudit audit, IUnitOfWork unitOfWork, TimeProvider timeProvider, ILogger<DiscountHandlers> logger)
    {
        this.guard = guard;
        this.billing = billing;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<IReadOnlyList<DiscountRow>> Handle(ListDiscountsQuery request, CancellationToken cancellationToken)
    {
        await this.guard.RequireStaffAsync(cancellationToken).ConfigureAwait(false);
        return [.. (await this.billing.ListDiscountsAsync(cancellationToken).ConfigureAwait(false))
            .OrderByDescending(f => f.IsActive).ThenBy(f => f.Name, Turkish).ThenBy(f => f.Id)
            .Select(Row)];
    }

    public async Task<DiscountRow> Handle(CreateDiscountCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireFounderAsync(cancellationToken).ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync([FeeScheduleHandlers.CatalogKey(staff.TenantId)], cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            await this.EnsureNameFreeAsync(request.Name!, null, cancellationToken).ConfigureAwait(false);
            DiscountDefinition fee = DiscountDefinition.Create(staff.TenantId, request.Name!, BillingRules.ParsePercent(request.Percent), this.timeProvider.GetUtcNow(), staff.UserId);
            this.billing.AddDiscount(fee);
            this.audit.Record(AuditAction.DiscountCreated, staff.TenantId, staff.UserId, nameof(DiscountDefinition), fee.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("İndirim eklendi: okul {TenantId}, indirim {DiscountId}, kişi {UserId}", staff.TenantId, fee.Id, staff.UserId);
            return Row(fee);
        }
    }

    public async Task<DiscountRow> Handle(UpdateDiscountCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireFounderAsync(cancellationToken).ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync([FeeScheduleHandlers.CatalogKey(staff.TenantId)], cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            DiscountDefinition fee = await this.billing.FindDiscountAsync(request.Id, cancellationToken).ConfigureAwait(false)
                ?? throw await this.guard.NotFoundAsync(BillingMessages.DiscountNotFound).ConfigureAwait(false);
            if (fee.IsActive)
            {
                await this.EnsureNameFreeAsync(request.Name!, fee.Id, cancellationToken).ConfigureAwait(false);
            }

            fee.Update(request.Name!, BillingRules.ParsePercent(request.Percent), this.timeProvider.GetUtcNow(), staff.UserId);
            this.audit.Record(AuditAction.DiscountUpdated, staff.TenantId, staff.UserId, nameof(DiscountDefinition), fee.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("İndirim düzenlendi: okul {TenantId}, indirim {DiscountId}, kişi {UserId}", staff.TenantId, fee.Id, staff.UserId);
            return Row(fee);
        }
    }

    public async Task<DiscountRow> Handle(SetDiscountActiveCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        BillingStaff staff = await this.guard.RequireFounderAsync(cancellationToken).ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync([FeeScheduleHandlers.CatalogKey(staff.TenantId)], cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            DiscountDefinition fee = await this.billing.FindDiscountAsync(request.Id, cancellationToken).ConfigureAwait(false)
                ?? throw await this.guard.NotFoundAsync(BillingMessages.DiscountNotFound).ConfigureAwait(false);
            if (request.Active && !fee.IsActive)
            {
                await this.EnsureNameFreeAsync(fee.Name, fee.Id, cancellationToken).ConfigureAwait(false);
            }

            if (fee.SetActive(request.Active, this.timeProvider.GetUtcNow(), staff.UserId))
            {
                this.audit.Record(request.Active ? AuditAction.DiscountActivated : AuditAction.DiscountDeactivated, staff.TenantId, staff.UserId, nameof(DiscountDefinition), fee.Id);
                await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
                this.logger.LogInformation("İndirim {State}: okul {TenantId}, indirim {DiscountId}, kişi {UserId}", request.Active ? "etkin" : "pasif", staff.TenantId, fee.Id, staff.UserId);
            }

            return Row(fee);
        }
    }

    private static DiscountRow Row(DiscountDefinition fee) => new(fee.Id, fee.Name, fee.Percent, fee.IsActive);

    private async Task EnsureNameFreeAsync(string name, Guid? self, CancellationToken cancellationToken)
    {
        string clean = name.Trim();
        if ((await this.billing.ListDiscountsAsync(cancellationToken).ConfigureAwait(false))
            .Any(f => f.IsActive && f.Id != self && Turkish.Equals(f.Name.Trim(), clean)))
        {
            throw await this.guard.RuleAsync(BillingMessages.DiscountNameTaken).ConfigureAwait(false);
        }
    }
}
