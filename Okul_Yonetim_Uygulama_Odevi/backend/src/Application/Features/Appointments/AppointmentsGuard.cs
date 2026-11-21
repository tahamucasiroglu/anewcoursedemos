using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration.ClassroomTeachers;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Organization.Queries.GetClassroomById;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListGuardianContacts;
using OkulYonetim.Application.Features.Students.Queries.ListMyChildren;
using OkulYonetim.Application.Features.Students.Queries.ListStudentBriefs;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Appointments;

/// <summary>
/// Randevunun yetki ve ilişki kapısı (spec 0016 C-2, gereksinim 9; <c>docs/security.md</c>). Rol her istekte veritabanından okunur:
/// müsaitlik ve yanıt işlemlerinde Öğretmen rolü (bir şubede; <see cref="TeacherClassroomAccess"/> ile aynı kural), talep işlemlerinde
/// Veli rolü yoksa 403. İlişki yoksa 404 — "var ama sizin değil" sızdırılmaz.
/// <para>
/// Velinin çocuğu: "Çocuklarım" sorgusundaki (bağlı ve ayrılmamış) çocuk. Çocuğun öğretmenleri (<see cref="ChildTeachersAsync"/>):
/// öğrenci görünür ve bir sınıfta, sınıf etkin, öğretmen o sınıfa atanmış (0010 sınıf öğretmeni bağı — mesajlaşma ve yoklamayla aynı
/// ilişki) ve sınıfın şubesinde hâlâ etkin Öğretmen. Yeni talepte ayrıca veli o çocuğun okulda etkin velisidir. İlişki biterse
/// (öğrenci sınıftan ayrıldı, öğretmen sınıftan alındı, veli bağı kalktı) yeni talep reddedilir; var olan randevu talep eden veliye
/// ve öğretmene görünür kalır (işleyicilerde: randevu sahipliğine göre).
/// </para>
/// </summary>
public sealed class AppointmentsGuard
{
    private readonly ICurrentSession session;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly IClassroomTeacherRepository teachers;
    private readonly ISender sender;
    private readonly ILocalizationService localization;

    public AppointmentsGuard(
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

    /// <summary>Çağıran kişi ve bu okuldaki Öğretmen / Veli rolleri; okul seçilmemişse 403.</summary>
    public async Task<AppointmentsCaller> CallerAsync(CancellationToken cancellationToken)
    {
        if (this.session.TenantId is not { } tenantId || this.session.UserId is not { } userId)
        {
            throw await this.ForbiddenAsync(AppointmentsMessages.Forbidden).ConfigureAwait(false);
        }

        IReadOnlyList<RoleAssignment> roles = await this.roleAssignments.ListInCurrentTenantAsync(userId, cancellationToken).ConfigureAwait(false);
        return new AppointmentsCaller(
            tenantId,
            userId,
            roles.Any(r => r.Role == Role.Teacher && r.BranchId is not null),
            roles.Any(r => r.Role == Role.Guardian));
    }

    /// <summary>Öğretmen rolü olan çağıran; değilse 403.</summary>
    public async Task<AppointmentsCaller> RequireTeacherAsync(CancellationToken cancellationToken)
    {
        AppointmentsCaller caller = await this.CallerAsync(cancellationToken).ConfigureAwait(false);
        return caller.IsTeacher ? caller : throw await this.ForbiddenAsync(AppointmentsMessages.TeacherOnly).ConfigureAwait(false);
    }

    /// <summary>Veli rolü olan çağıran; değilse 403.</summary>
    public async Task<AppointmentsCaller> RequireGuardianAsync(CancellationToken cancellationToken)
    {
        AppointmentsCaller caller = await this.CallerAsync(cancellationToken).ConfigureAwait(false);
        return caller.IsGuardian ? caller : throw await this.ForbiddenAsync(AppointmentsMessages.GuardianOnly).ConfigureAwait(false);
    }

    /// <summary>Velinin bağlı ve ayrılmamış çocukları; veli değilse boş.</summary>
    public async Task<IReadOnlyList<ChildRow>> ChildrenAsync(AppointmentsCaller caller, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(caller);
        return caller.IsGuardian
            ? await this.sender.Send(new ListMyChildrenQuery(), cancellationToken).ConfigureAwait(false)
            : [];
    }

    /// <summary>Velinin bu çocuğu; değilse 404.</summary>
    public async Task<ChildRow> RequireChildAsync(AppointmentsCaller caller, Guid studentId, CancellationToken cancellationToken) =>
        (await this.ChildrenAsync(caller, cancellationToken).ConfigureAwait(false)).FirstOrDefault(c => c.Id == studentId)
            ?? throw await this.NotFoundAsync(AppointmentsMessages.ChildNotFound).ConfigureAwait(false);

    /// <summary>Çocuğun sınıfının öğretmenleri (atanmış ve sınıfın şubesinde etkin Öğretmen; sınıf etkin), çağıran hariç.</summary>
    public async Task<IReadOnlyList<TeacherOption>> ChildTeachersAsync(Guid studentId, Guid excludeUserId, CancellationToken cancellationToken)
    {
        IReadOnlyList<StudentBrief> found = await this.sender.Send(new ListStudentBriefsQuery(Ids: [studentId]), cancellationToken).ConfigureAwait(false);
        if (found.Count == 0 || found[0].ClassroomId is not { } classroomId)
        {
            return [];
        }

        ClassroomSummary classroom;
        try
        {
            classroom = await this.sender.Send(new GetClassroomByIdQuery(classroomId), cancellationToken).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            return [];
        }

        if (!classroom.IsActive)
        {
            return [];
        }

        HashSet<Guid> active = [.. (await this.teachers.ListCandidatesAsync(classroom.BranchId, cancellationToken).ConfigureAwait(false)).Select(t => t.UserId)];
        return [.. (await this.teachers.ListAssignedAsync(classroom.Id, cancellationToken).ConfigureAwait(false))
            .Where(t => active.Contains(t.UserId) && t.UserId != excludeUserId)];
    }

    /// <summary>
    /// Yeni talep için ilişki: çocuk velinin (bağlı, ayrılmamış), veli çocuğun okulda etkin velisi ve öğretmen çocuğun sınıfının
    /// öğretmeni; değilse 404 (çocuk ya da öğretmen bulunamadı).
    /// </summary>
    public async Task<TeacherOption> RequireRelatedTeacherAsync(AppointmentsCaller caller, Guid studentId, Guid teacherUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(caller);
        await this.RequireChildAsync(caller, studentId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<GuardianContact> guardians = await this.sender.Send(new ListGuardianContactsQuery([studentId]), cancellationToken).ConfigureAwait(false);
        if (!guardians.Any(g => g.UserId == caller.UserId))
        {
            throw await this.NotFoundAsync(AppointmentsMessages.ChildNotFound).ConfigureAwait(false);
        }

        return (await this.ChildTeachersAsync(studentId, caller.UserId, cancellationToken).ConfigureAwait(false)).FirstOrDefault(t => t.UserId == teacherUserId)
            ?? throw await this.NotFoundAsync(AppointmentsMessages.TeacherNotFound).ConfigureAwait(false);
    }

    public async Task<ForbiddenException> ForbiddenAsync(string key) =>
        new(await this.localization.AppointmentsTextAsync(key).ConfigureAwait(false));

    public async Task<NotFoundException> NotFoundAsync(string key) =>
        new(await this.localization.AppointmentsTextAsync(key).ConfigureAwait(false));

    public async Task<BusinessException> BusinessAsync(string key) =>
        new(await this.localization.AppointmentsTextAsync(key).ConfigureAwait(false));
}
