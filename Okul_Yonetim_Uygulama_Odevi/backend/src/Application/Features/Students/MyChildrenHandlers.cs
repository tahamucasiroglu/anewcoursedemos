using System.Globalization;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Features.Students.Queries.GetMyChild;
using OkulYonetim.Application.Features.Students.Queries.ListMyChildren;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Velinin "Çocuklarım" uçları (spec 0009 gereksinim 2, 8, 11): yalnızca seçili okulda veli rolü olan kişi (değilse 403).
/// Veli rolü şube düzeyinde olduğu için şube filtresi tek başına yetmez (plan Risks 4): öğrenci yalnızca oturumdaki kişinin
/// <b>bağı</b> üzerinden okunur ve "ayrıldı" öğrenci düşer — bağı olmayan (aynı şubedeki bile) öğrenci 404'tür. Kart salt
/// okunurdur; diğer velilerin bilgisi ve kardeş kartları yoktur. Her kart okunuşu audit'e geçer.
/// </summary>
public sealed class MyChildrenHandlers :
    IRequestHandler<ListMyChildrenQuery, IReadOnlyList<ChildRow>>,
    IRequestHandler<GetMyChildQuery, ChildCard>
{
    private static readonly StringComparer TurkishOrder = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);

    private readonly ICurrentSession session;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly IStudentRepository students;
    private readonly IStudentGuardianRepository guardians;
    private readonly IBranchRepository branches;
    private readonly IClassroomRepository classrooms;
    private readonly StudentHealth health;
    private readonly AdminAudit audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;

    public MyChildrenHandlers(
        ICurrentSession session,
        IRoleAssignmentRepository roleAssignments,
        IStudentRepository students,
        IStudentGuardianRepository guardians,
        IBranchRepository branches,
        IClassroomRepository classrooms,
        StudentHealth health,
        AdminAudit audit,
        IUnitOfWork unitOfWork,
        ILocalizationService localization)
    {
        this.session = session;
        this.roleAssignments = roleAssignments;
        this.students = students;
        this.guardians = guardians;
        this.branches = branches;
        this.classrooms = classrooms;
        this.health = health;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
    }

    public async Task<IReadOnlyList<ChildRow>> Handle(ListMyChildrenQuery request, CancellationToken cancellationToken)
    {
        (_, Guid userId) = await this.RequireGuardianAsync(cancellationToken).ConfigureAwait(false);
        return [.. (await this.guardians.ListChildrenAsync(userId, cancellationToken).ConfigureAwait(false))
            .OrderBy(c => c.FullName, TurkishOrder).ThenBy(c => c.Id)];
    }

    public async Task<ChildCard> Handle(GetMyChildQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid userId) = await this.RequireGuardianAsync(cancellationToken).ConfigureAwait(false);
        Student? student = await this.guardians.IsLinkedAsync(request.Id, userId, cancellationToken).ConfigureAwait(false)
            ? await this.students.FindAsync(request.Id, cancellationToken).ConfigureAwait(false)
            : null;
        Branch? branch = student is { IsActive: true }
            ? await this.branches.FindVisibleAsync(student.BranchId, cancellationToken).ConfigureAwait(false)
            : null;
        if (student is null || branch is null)
        {
            throw new NotFoundException(await this.localization.StudentTextAsync(StudentsMessages.ChildNotFound).ConfigureAwait(false));
        }

        Classroom? classroom = student.ClassroomId is { } classroomId
            ? await this.classrooms.FindAsync(classroomId, cancellationToken).ConfigureAwait(false)
            : null;
        StudentHealthView healthView = this.health.Read(student);
        IReadOnlyList<PickupPersonView> pickups = [.. (await this.students.ListPickupPersonsAsync(student.Id, cancellationToken).ConfigureAwait(false))
            .Select(p => new PickupPersonView(p.FullName, p.Relationship, p.Phone))];
        this.audit.Record(AuditAction.StudentViewed, tenantId, userId, nameof(Student), student.Id);
        if (healthView.Available && StudentHealth.HasAny(student))
        {
            this.audit.Record(AuditAction.StudentHealthRead, tenantId, userId, nameof(Student), student.Id);
        }

        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ChildCard(student.Id, student.FullName, student.BirthDate, student.SchoolNumber, branch.Name, classroom?.Name, healthView, pickups);
    }

    private async Task<(Guid TenantId, Guid UserId)> RequireGuardianAsync(CancellationToken cancellationToken)
    {
        if (this.session.TenantId is { } tenantId && this.session.UserId is { } userId)
        {
            IReadOnlyList<RoleAssignment> roles = await this.roleAssignments.ListInCurrentTenantAsync(userId, cancellationToken).ConfigureAwait(false);
            if (roles.Any(r => r.Role == Role.Guardian))
            {
                return (tenantId, userId);
            }
        }

        throw new ForbiddenException(await this.localization.StudentTextAsync(StudentsMessages.GuardianOnly).ConfigureAwait(false));
    }
}
