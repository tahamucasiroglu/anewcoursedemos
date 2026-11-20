using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Communication.Messages;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListGuardianContacts;
using OkulYonetim.Application.Features.Students.Queries.ListMyChildren;
using OkulYonetim.Application.Features.Students.Queries.ListStudentBriefs;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// İletişim modülünün yetki ve ilişki kapısı (spec 0011; <c>docs/security.md</c> AuthN/AuthZ 4). Rol her istekte
/// veritabanından okunur (<c>AdminGuard</c> / <c>AttendanceGuard</c> kalıbı): rol yoksa 403. İlişki (öğretmen ↔ sınıf,
/// veli ↔ öğrenci) yoksa 404 — "var ama sizin değil" sızdırılmaz.
/// <para>
/// Öğretmen–veli–öğrenci ilişkisi (<see cref="IsRelatedAsync"/>, gereksinim 5): öğrenci görünür ve ayrılmamış, bir sınıfta;
/// öğretmen o sınıfa atanmış (0010'un sınıf öğretmeni bağı — yoklamayla aynı ilişki), sınıf etkin ve öğretmen sınıfın
/// şubesinde hâlâ etkin Öğretmen; veli öğrenciye bağlı ve okulda etkin veli (bağ + etkin üyelik + Veli rolü). Biri bozulursa
/// eski konuşma okunur ama yeni mesaj gönderilemez.
/// </para>
/// </summary>
public sealed class CommunicationGuard
{
    private readonly ICurrentSession session;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly IClassroomTeacherRepository teachers;
    private readonly ISender sender;
    private readonly ILocalizationService localization;

    public CommunicationGuard(
        ICurrentSession session,
        IRoleAssignmentRepository roleAssignments,
        IClassroomTeacherRepository teachers,
        ISender sender,
        ILocalizationService localization)
    {
        this.session = session;
        this.roleAssignments = roleAssignments;
        this.teachers = teachers;
        this.sender = sender;
        this.localization = localization;
    }

    /// <summary>Çağıran kişi ve bu okuldaki iletişim rolleri; okul seçilmemişse 403.</summary>
    public async Task<CommunicationCaller> CallerAsync(CancellationToken cancellationToken)
    {
        if (this.session.TenantId is not { } tenantId || this.session.UserId is not { } userId)
        {
            throw await this.ForbiddenAsync().ConfigureAwait(false);
        }

        IReadOnlyList<RoleAssignment> roles = await this.roleAssignments.ListInCurrentTenantAsync(userId, cancellationToken).ConfigureAwait(false);
        HashSet<Guid> teacherBranches = [.. roles.Where(r => r.Role == Role.Teacher && r.BranchId is not null).Select(r => r.BranchId!.Value)];
        return new CommunicationCaller(tenantId, userId, this.session.IsFounder, teacherBranches, roles.Any(r => r.Role == Role.Guardian));
    }

    /// <summary>Velinin bağlı ve ayrılmamış çocukları ("Çocuklarım" sorgusu); veli değilse boş.</summary>
    public async Task<IReadOnlyList<ChildRow>> ChildrenAsync(CommunicationCaller caller, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(caller);
        return caller.IsGuardian
            ? await this.sender.Send(new ListMyChildrenQuery(), cancellationToken).ConfigureAwait(false)
            : [];
    }

    /// <summary>Öğretmenin atandığı, etkin ve şubesinde hâlâ öğretmen olduğu sınıflar; öğretmen değilse boş.</summary>
    public async Task<IReadOnlyList<TeacherClassroomRow>> TeacherClassroomsAsync(CommunicationCaller caller, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(caller);
        return caller.IsTeacher
            ? [.. (await this.teachers.ListForTeacherAsync(caller.UserId, cancellationToken).ConfigureAwait(false))
                .Where(c => c.IsActive && caller.TeacherBranchIds.Contains(c.BranchId))]
            : [];
    }

    /// <summary>Sınıfın şubesinde etkin Öğretmen olan atanmış öğretmenler (mesaj yazılabilecekler).</summary>
    public async Task<IReadOnlyList<ContactPerson>> ClassroomTeachersAsync(Guid classroomId, Guid branchId, CancellationToken cancellationToken)
    {
        HashSet<Guid> active = [.. (await this.teachers.ListCandidatesAsync(branchId, cancellationToken).ConfigureAwait(false)).Select(t => t.UserId)];
        return [.. (await this.teachers.ListAssignedAsync(classroomId, cancellationToken).ConfigureAwait(false))
            .Where(t => active.Contains(t.UserId))
            .Select(t => new ContactPerson(t.UserId, t.FullName))];
    }

    /// <summary>Öğretmen–veli–öğrenci ilişkisi sürüyor mu (yeni konuşma ve her mesajda).</summary>
    public async Task<bool> IsRelatedAsync(Guid teacherUserId, Guid guardianUserId, Guid studentId, CancellationToken cancellationToken)
    {
        if (teacherUserId == guardianUserId)
        {
            return false;
        }

        IReadOnlyList<StudentBrief> found = await this.sender.Send(new ListStudentBriefsQuery(Ids: [studentId]), cancellationToken).ConfigureAwait(false);
        StudentBrief? student = found.Count > 0 ? found[0] : null;
        if (student?.ClassroomId is not { } classroomId)
        {
            return false;
        }

        TeacherClassroomRow? classroom = (await this.teachers.ListForTeacherAsync(teacherUserId, cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(c => c.ClassroomId == classroomId && c.IsActive);
        if (classroom is null)
        {
            return false;
        }

        if (!(await this.teachers.ListCandidatesAsync(classroom.BranchId, cancellationToken).ConfigureAwait(false)).Any(t => t.UserId == teacherUserId))
        {
            return false;
        }

        IReadOnlyList<GuardianContact> guardians = await this.sender.Send(new ListGuardianContactsQuery([studentId]), cancellationToken).ConfigureAwait(false);
        return guardians.Any(g => g.UserId == guardianUserId);
    }

    public async Task<ForbiddenException> ForbiddenAsync() =>
        new(await this.localization.CommunicationTextAsync(CommunicationMessages.Forbidden).ConfigureAwait(false));

    public async Task<NotFoundException> NotFoundAsync(string key) =>
        new(await this.localization.CommunicationTextAsync(key).ConfigureAwait(false));
}
