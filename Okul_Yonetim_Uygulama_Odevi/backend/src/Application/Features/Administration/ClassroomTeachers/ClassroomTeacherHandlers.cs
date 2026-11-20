using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Administration.ClassroomTeachers;

/// <summary>
/// Sınıf öğretmeni ataması (spec 0010 gereksinim 1, C-3): yalnızca seçili okulun kurucusu. Sınıf kurum filtresinden okunur
/// (başka okulun sınıfı 404). Atanabilecek kişi, seçili okulda etkin üyeliği ve sınıfın şubesinde Öğretmen rolü olan kişidir;
/// listede biri bile uygun değilse hiçbir şey değişmez. Okul başına sıra içinde (rol değişiklikleriyle aynı sıra) denetlenir
/// ve her atama/kaldırma audit'e yazılır.
/// </summary>
public sealed class ClassroomTeacherHandlers :
    IRequestHandler<ListClassroomTeachersQuery, ClassroomTeachersView>,
    IRequestHandler<SetClassroomTeachersCommand, ClassroomTeachersView>
{
    private static readonly StringComparer TurkishOrder = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);
    private readonly AdminGuard guard;
    private readonly AdminAudit audit;
    private readonly IClassroomRepository classrooms;
    private readonly IBranchRepository branches;
    private readonly IClassroomTeacherRepository teachers;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ClassroomTeacherHandlers> logger;

    public ClassroomTeacherHandlers(
        AdminGuard guard,
        AdminAudit audit,
        IClassroomRepository classrooms,
        IBranchRepository branches,
        IClassroomTeacherRepository teachers,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<ClassroomTeacherHandlers> logger)
    {
        this.guard = guard;
        this.audit = audit;
        this.classrooms = classrooms;
        this.branches = branches;
        this.teachers = teachers;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<ClassroomTeachersView> Handle(ListClassroomTeachersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await this.guard.RequireFounderAsync().ConfigureAwait(false);
        Classroom classroom = await this.FindAsync(request.ClassroomId, cancellationToken).ConfigureAwait(false);
        return await this.ViewAsync(classroom, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ClassroomTeachersView> Handle(SetClassroomTeachersCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Classroom classroom = await this.FindAsync(request.ClassroomId, cancellationToken).ConfigureAwait(false);
            HashSet<Guid> wanted = [.. request.TeacherUserIds!];
            IReadOnlyList<ClassroomTeacher> current = await this.teachers.ListForClassroomAsync(classroom.Id, cancellationToken).ConfigureAwait(false);
            HashSet<Guid> already = [.. current.Select(t => t.TeacherUserId)];
            HashSet<Guid> candidates = [.. (await this.teachers.ListCandidatesAsync(classroom.BranchId, cancellationToken).ConfigureAwait(false)).Select(c => c.UserId)];
            if (wanted.Any(id => !already.Contains(id) && !candidates.Contains(id)))
            {
                throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.TeacherUnavailable).ConfigureAwait(false));
            }

            DateTimeOffset now = this.timeProvider.GetUtcNow();
            int added = 0;
            int removed = 0;
            foreach (ClassroomTeacher assignment in current.Where(t => !wanted.Contains(t.TeacherUserId)))
            {
                this.teachers.Remove(assignment);
                this.audit.Record(AuditAction.ClassroomTeacherRemoved, tenantId, actorId, nameof(Classroom), classroom.Id, "teacher=" + assignment.TeacherUserId.ToString("D"));
                removed++;
            }

            foreach (Guid teacherId in wanted.Where(id => !already.Contains(id)))
            {
                this.teachers.Add(ClassroomTeacher.Assign(tenantId, classroom.BranchId, classroom.Id, teacherId, now));
                this.audit.Record(AuditAction.ClassroomTeacherAssigned, tenantId, actorId, nameof(Classroom), classroom.Id, "teacher=" + teacherId.ToString("D"));
                added++;
            }

            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Sınıf öğretmenleri güncellendi: {ClassroomId}, eklenen {Added}, kaldırılan {Removed}", classroom.Id, added, removed);
            return await this.ViewAsync(classroom, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<Classroom> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await this.classrooms.FindAsync(id, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException(await this.localization.GetAsync(AdministrationMessages.ClassroomNotFound).ConfigureAwait(false));

    private async Task<ClassroomTeachersView> ViewAsync(Classroom classroom, CancellationToken cancellationToken)
    {
        Domain.Organization.Branch branch = await this.branches.FindVisibleAsync(classroom.BranchId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sınıfın şubesi okunamadı.");
        IReadOnlyList<TeacherOption> assigned = await this.teachers.ListAssignedAsync(classroom.Id, cancellationToken).ConfigureAwait(false);
        HashSet<Guid> assignedIds = [.. assigned.Select(a => a.UserId)];
        IReadOnlyList<TeacherOption> candidates = await this.teachers.ListCandidatesAsync(classroom.BranchId, cancellationToken).ConfigureAwait(false);
        return new ClassroomTeachersView(
            classroom.Id,
            classroom.Name,
            branch.Name,
            [.. assigned.OrderBy(t => t.FullName, TurkishOrder).ThenBy(t => t.UserId)],
            [.. candidates.Where(c => !assignedIds.Contains(c.UserId)).OrderBy(t => t.FullName, TurkishOrder).ThenBy(t => t.UserId)]);
    }
}
