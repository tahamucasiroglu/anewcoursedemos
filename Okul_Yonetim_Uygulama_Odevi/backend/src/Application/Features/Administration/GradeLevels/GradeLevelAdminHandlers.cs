using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Administration.GradeLevels;

/// <summary>
/// Sınıf seviyesi yönetimi (spec 0008 gereksinim 3): ekleme, düzenleme, pasifleştirme/etkinleştirme ve yönetim listesi.
/// Yalnızca seçili okulun kurucusu; başka okulun seviyesi "bulunamadı" (filtre). Aynı okulda aynı adlı (Türkçe harf
/// kuralıyla, harf farkı gözetmeden) ikinci etkin seviye olamaz. Değişiklikler okul başına sıralı ve audit'li.
/// </summary>
public sealed class GradeLevelAdminHandlers :
    IRequestHandler<CreateGradeLevelCommand, GradeLevelAdminView>,
    IRequestHandler<UpdateGradeLevelCommand, GradeLevelAdminView>,
    IRequestHandler<SetGradeLevelActiveCommand, GradeLevelAdminView>,
    IRequestHandler<ListAdminGradeLevelsQuery, IReadOnlyList<GradeLevelAdminView>>
{
    private static readonly StringComparer Turkish = StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);
    private readonly AdminGuard guard;
    private readonly AdminAudit audit;
    private readonly IGradeLevelRepository gradeLevels;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<GradeLevelAdminHandlers> logger;

    public GradeLevelAdminHandlers(
        AdminGuard guard,
        AdminAudit audit,
        IGradeLevelRepository gradeLevels,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<GradeLevelAdminHandlers> logger)
    {
        this.guard = guard;
        this.audit = audit;
        this.gradeLevels = gradeLevels;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<GradeLevelAdminView> Handle(CreateGradeLevelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            await this.EnsureNameFreeAsync(request.Name, null, cancellationToken).ConfigureAwait(false);
            GradeLevel level = GradeLevel.Create(tenantId, request.Name, request.SortOrder!.Value, this.timeProvider.GetUtcNow());
            this.gradeLevels.Add(level);
            this.audit.Record(AuditAction.GradeLevelCreated, tenantId, actorId, nameof(GradeLevel), level.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Sınıf seviyesi eklendi: {GradeLevelId}, okul {TenantId}", level.Id, tenantId);
            return View(level);
        }
    }

    public async Task<GradeLevelAdminView> Handle(UpdateGradeLevelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            GradeLevel level = await this.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (level.IsActive)
            {
                await this.EnsureNameFreeAsync(request.Name, level.Id, cancellationToken).ConfigureAwait(false);
            }

            level.Update(request.Name, request.SortOrder!.Value);
            this.audit.Record(AuditAction.GradeLevelUpdated, tenantId, actorId, nameof(GradeLevel), level.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Sınıf seviyesi düzenlendi: {GradeLevelId}", level.Id);
            return View(level);
        }
    }

    public async Task<GradeLevelAdminView> Handle(SetGradeLevelActiveCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            GradeLevel level = await this.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (request.Active && !level.IsActive)
            {
                await this.EnsureNameFreeAsync(level.Name, level.Id, cancellationToken).ConfigureAwait(false);
                level.Activate();
                this.audit.Record(AuditAction.GradeLevelActivated, tenantId, actorId, nameof(GradeLevel), level.Id);
            }
            else if (!request.Active && level.IsActive)
            {
                level.Deactivate();
                this.audit.Record(AuditAction.GradeLevelDeactivated, tenantId, actorId, nameof(GradeLevel), level.Id);
            }

            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Sınıf seviyesi durumu: {GradeLevelId} etkin={Active}", level.Id, level.IsActive);
            return View(level);
        }
    }

    public async Task<IReadOnlyList<GradeLevelAdminView>> Handle(ListAdminGradeLevelsQuery request, CancellationToken cancellationToken)
    {
        await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IReadOnlyList<GradeLevel> all = await this.gradeLevels.ListAsync(cancellationToken).ConfigureAwait(false);
        return [.. Ordered(all).Select(View)];
    }

    /// <summary>Seviyelerin ortak sırası: sıra numarası, eşitse ad (Türkçe).</summary>
    internal static IEnumerable<GradeLevel> Ordered(IEnumerable<GradeLevel> levels) =>
        levels.OrderBy(g => g.SortOrder).ThenBy(g => g.Name, Turkish);

    private static GradeLevelAdminView View(GradeLevel level) => new(level.Id, level.Name, level.SortOrder, level.IsActive);

    private async Task<GradeLevel> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await this.gradeLevels.FindAsync(id, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException(await this.localization.GetAsync(AdministrationMessages.GradeLevelNotFound).ConfigureAwait(false));

    private async Task EnsureNameFreeAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (await this.gradeLevels.ActiveNameExistsAsync(name, exceptId, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.GradeLevelNameExists).ConfigureAwait(false));
        }
    }
}
