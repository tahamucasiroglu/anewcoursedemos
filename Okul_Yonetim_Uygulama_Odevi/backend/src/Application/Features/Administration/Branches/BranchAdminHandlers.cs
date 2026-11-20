using FluentValidation;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Administration.Branches;

/// <summary>
/// Şube yönetimi (spec 0005 gereksinim 2): ekleme, düzenleme, pasifleştirme/etkinleştirme ve yönetim listesi. Yalnızca
/// seçili okulun kurucusu; başka okulun şubesi "bulunamadı" (filtre). Aynı okulda aynı adlı (harf farkı gözetmeden)
/// ikinci etkin şube olamaz; son etkin şube pasifleştirilemez. Her değişiklik audit'e yazılır. Değişiklikler okul
/// başına sıralıdır (ad ve "son etkin şube" kontrolleri eşzamanlı isteklerle atlatılamaz).
/// </summary>
public sealed class BranchAdminHandlers :
    IRequestHandler<CreateBranchCommand, BranchAdminView>,
    IRequestHandler<UpdateBranchCommand, BranchAdminView>,
    IRequestHandler<SetBranchActiveCommand, BranchAdminView>,
    IRequestHandler<ListAdminBranchesQuery, IReadOnlyList<BranchAdminView>>
{
    private readonly AdminGuard guard;
    private readonly AdminAudit audit;
    private readonly IBranchRepository branches;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;

    public BranchAdminHandlers(AdminGuard guard, AdminAudit audit, IBranchRepository branches, IUnitOfWork unitOfWork, ILocalizationService localization, TimeProvider timeProvider)
    {
        this.guard = guard;
        this.audit = audit;
        this.branches = branches;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
    }

    public async Task<BranchAdminView> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            await this.EnsureNameFreeAsync(request.Name, null, cancellationToken).ConfigureAwait(false);
            PhoneNumber.TryNormalize(request.Phone, out string? phone);
            Branch branch = Branch.Create(tenantId, request.Name, this.timeProvider.GetUtcNow(), request.Address, phone);
            this.branches.Add(branch);
            this.audit.Record(AuditAction.BranchCreated, tenantId, actorId, nameof(Branch), branch.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            return View(branch);
        }
    }

    public async Task<BranchAdminView> Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Branch branch = await this.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (branch.IsActive)
            {
                await this.EnsureNameFreeAsync(request.Name, branch.Id, cancellationToken).ConfigureAwait(false);
            }

            PhoneNumber.TryNormalize(request.Phone, out string? phone);
            branch.UpdateDetails(request.Name, request.Address, phone);
            this.audit.Record(AuditAction.BranchUpdated, tenantId, actorId, nameof(Branch), branch.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            return View(branch);
        }
    }

    public async Task<BranchAdminView> Handle(SetBranchActiveCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Branch branch = await this.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (request.Active && !branch.IsActive)
            {
                await this.EnsureNameFreeAsync(branch.Name, branch.Id, cancellationToken).ConfigureAwait(false);
                branch.Activate();
                this.audit.Record(AuditAction.BranchActivated, tenantId, actorId, nameof(Branch), branch.Id);
            }
            else if (!request.Active && branch.IsActive)
            {
                if (await this.branches.CountActiveAsync(cancellationToken).ConfigureAwait(false) <= 1)
                {
                    throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.LastActiveBranch).ConfigureAwait(false));
                }

                branch.Deactivate();
                this.audit.Record(AuditAction.BranchDeactivated, tenantId, actorId, nameof(Branch), branch.Id);
            }

            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            return View(branch);
        }
    }

    public async Task<IReadOnlyList<BranchAdminView>> Handle(ListAdminBranchesQuery request, CancellationToken cancellationToken)
    {
        await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IReadOnlyList<Branch> all = await this.branches.ListVisibleAsync(cancellationToken).ConfigureAwait(false);
        StringComparer turkish = StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);
        return [.. all.OrderByDescending(b => b.IsActive).ThenBy(b => b.Name, turkish).Select(View)];
    }

    private static BranchAdminView View(Branch branch) => new(branch.Id, branch.Name, branch.Address, branch.Phone, branch.IsActive);

    private async Task<Branch> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await this.branches.FindVisibleAsync(id, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException(await this.localization.GetAsync(AdministrationMessages.BranchNotFound).ConfigureAwait(false));

    private async Task EnsureNameFreeAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (await this.branches.ActiveNameExistsAsync(name, exceptId, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.BranchNameExists).ConfigureAwait(false));
        }
    }
}
