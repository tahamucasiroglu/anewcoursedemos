using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Administration.Classrooms;

/// <summary>
/// Sınıf (şube-sınıf eşleşmesi) yönetimi (spec 0008 gereksinim 6): açma, düzenleme (ad, seviye), pasifleştirme/
/// etkinleştirme ve dönem bazlı yönetim listesi. Yalnızca seçili okulun kurucusu. Şube ve seviye filtreli okunur: başka
/// okulun şubesi/seviyesi "bulunamadı ya da pasif" ile reddedilir. Aynı dönem ve şubede aynı adlı (Türkçe harf kuralıyla)
/// ikinci etkin sınıf olamaz; kural okul başına sıra içinde denetlenir. Her değişiklik audit'li.
/// </summary>
public sealed class ClassroomAdminHandlers :
    IRequestHandler<CreateClassroomCommand, ClassroomAdminView>,
    IRequestHandler<UpdateClassroomCommand, ClassroomAdminView>,
    IRequestHandler<SetClassroomActiveCommand, ClassroomAdminView>,
    IRequestHandler<ListAdminClassroomsQuery, ClassroomAdminList>
{
    private static readonly StringComparer Turkish = StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);
    private readonly AdminGuard guard;
    private readonly AdminAudit audit;
    private readonly IClassroomRepository classrooms;
    private readonly ITermRepository terms;
    private readonly IBranchRepository branches;
    private readonly IGradeLevelRepository gradeLevels;
    private readonly SchoolCalendar calendar;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ClassroomAdminHandlers> logger;

    public ClassroomAdminHandlers(
        AdminGuard guard,
        AdminAudit audit,
        IClassroomRepository classrooms,
        ITermRepository terms,
        IBranchRepository branches,
        IGradeLevelRepository gradeLevels,
        SchoolCalendar calendar,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<ClassroomAdminHandlers> logger)
    {
        this.guard = guard;
        this.audit = audit;
        this.classrooms = classrooms;
        this.terms = terms;
        this.branches = branches;
        this.gradeLevels = gradeLevels;
        this.calendar = calendar;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<ClassroomAdminView> Handle(CreateClassroomCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Term term = await this.terms.FindAsync(request.TermId!.Value, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(await this.localization.GetAsync(AdministrationMessages.TermNotFound).ConfigureAwait(false));
            Branch? branch = await this.branches.FindVisibleAsync(request.BranchId!.Value, cancellationToken).ConfigureAwait(false);
            if (branch is not { IsActive: true })
            {
                throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.BranchUnavailable).ConfigureAwait(false));
            }

            GradeLevel level = await this.ActiveLevelAsync(request.GradeLevelId!.Value, cancellationToken).ConfigureAwait(false);
            await this.EnsureNameFreeAsync(term.Id, branch.Id, request.Name, null, cancellationToken).ConfigureAwait(false);
            Classroom classroom = Classroom.Create(tenantId, term.Id, branch.Id, level.Id, request.Name, this.timeProvider.GetUtcNow());
            this.classrooms.Add(classroom);
            this.audit.Record(AuditAction.ClassroomCreated, tenantId, actorId, nameof(Classroom), classroom.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Sınıf açıldı: {ClassroomId}, dönem {TermId}, şube {BranchId}", classroom.Id, term.Id, branch.Id);
            return View(classroom, branch, level);
        }
    }

    public async Task<ClassroomAdminView> Handle(UpdateClassroomCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Classroom classroom = await this.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
            Guid levelId = request.GradeLevelId!.Value;

            // Pasif seviye yeni seçilemez; sınıfın mevcut (sonradan pasifleşmiş) seviyesi ise korunabilir (gereksinim 3).
            GradeLevel level = levelId == classroom.GradeLevelId
                ? await this.gradeLevels.FindAsync(levelId, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Sınıfın seviyesi okunamadı.")
                : await this.ActiveLevelAsync(levelId, cancellationToken).ConfigureAwait(false);
            if (classroom.IsActive)
            {
                await this.EnsureNameFreeAsync(classroom.TermId, classroom.BranchId, request.Name, classroom.Id, cancellationToken).ConfigureAwait(false);
            }

            classroom.Update(request.Name, level.Id);
            this.audit.Record(AuditAction.ClassroomUpdated, tenantId, actorId, nameof(Classroom), classroom.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Sınıf düzenlendi: {ClassroomId}", classroom.Id);
            return await this.ViewAsync(classroom, level, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<ClassroomAdminView> Handle(SetClassroomActiveCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Classroom classroom = await this.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
            if (request.Active && !classroom.IsActive)
            {
                await this.EnsureNameFreeAsync(classroom.TermId, classroom.BranchId, classroom.Name, classroom.Id, cancellationToken).ConfigureAwait(false);
                classroom.Activate();
                this.audit.Record(AuditAction.ClassroomActivated, tenantId, actorId, nameof(Classroom), classroom.Id);
            }
            else if (!request.Active && classroom.IsActive)
            {
                classroom.Deactivate();
                this.audit.Record(AuditAction.ClassroomDeactivated, tenantId, actorId, nameof(Classroom), classroom.Id);
            }

            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Sınıf durumu: {ClassroomId} etkin={Active}", classroom.Id, classroom.IsActive);
            GradeLevel level = await this.gradeLevels.FindAsync(classroom.GradeLevelId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Sınıfın seviyesi okunamadı.");
            return await this.ViewAsync(classroom, level, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<ClassroomAdminList> Handle(ListAdminClassroomsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await this.guard.RequireFounderAsync().ConfigureAwait(false);
        Term? term;
        if (request.TermId is { } termId)
        {
            term = await this.terms.FindAsync(termId, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(await this.localization.GetAsync(AdministrationMessages.TermNotFound).ConfigureAwait(false));
        }
        else
        {
            term = SchoolCalendar.PickDefault(await this.terms.ListAsync(cancellationToken).ConfigureAwait(false), this.calendar.Today());
        }

        if (term is null)
        {
            return new ClassroomAdminList(null, []);
        }

        IReadOnlyList<Classroom> inTerm = await this.classrooms.ListByTermAsync(term.Id, cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, Branch> branchById = (await this.branches.ListVisibleAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(b => b.Id);
        Dictionary<Guid, GradeLevel> levelById = (await this.gradeLevels.ListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(g => g.Id);
        IEnumerable<ClassroomAdminView> items = inTerm
            .Where(c => request.BranchId is null || c.BranchId == request.BranchId)
            .Where(c => request.Active is null || c.IsActive == request.Active)
            .Select(c => View(c, branchById[c.BranchId], levelById[c.GradeLevelId]))
            .OrderBy(v => v.BranchName, Turkish)
            .ThenBy(v => v.GradeLevelSortOrder)
            .ThenBy(v => v.GradeLevelName, Turkish)
            .ThenBy(v => v.Name, Turkish);
        return new ClassroomAdminList(term.Id, [.. items]);
    }

    private static ClassroomAdminView View(Classroom c, Branch branch, GradeLevel level) =>
        new(c.Id, c.TermId, branch.Id, branch.Name, branch.IsActive, level.Id, level.Name, level.SortOrder, c.Name, c.IsActive);

    private async Task<ClassroomAdminView> ViewAsync(Classroom classroom, GradeLevel level, CancellationToken cancellationToken)
    {
        Branch branch = await this.branches.FindVisibleAsync(classroom.BranchId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sınıfın şubesi okunamadı.");
        return View(classroom, branch, level);
    }

    private async Task<Classroom> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await this.classrooms.FindAsync(id, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException(await this.localization.GetAsync(AdministrationMessages.ClassroomNotFound).ConfigureAwait(false));

    private async Task<GradeLevel> ActiveLevelAsync(Guid id, CancellationToken cancellationToken)
    {
        GradeLevel? level = await this.gradeLevels.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return level is { IsActive: true }
            ? level
            : throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.GradeLevelUnavailable).ConfigureAwait(false));
    }

    private async Task EnsureNameFreeAsync(Guid termId, Guid branchId, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (await this.classrooms.ActiveNameExistsAsync(termId, branchId, name, exceptId, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.ClassroomNameExists).ConfigureAwait(false));
        }
    }
}
