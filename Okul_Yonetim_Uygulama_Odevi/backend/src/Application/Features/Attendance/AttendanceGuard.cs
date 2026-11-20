using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Organization.Queries.GetClassroomById;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Yoklamanın üç katmanlı yetki kapısı (spec 0010 gereksinim 1–2, AC-10; <c>docs/security.md</c> AuthN/AuthZ 4). Her istekte
/// veritabanından: kişinin seçili okulda Öğretmen rolü olmalı (değilse 403 — veli, muhasebe, öğretmen olmayan kurucu).
/// Sınıf kurum + şube filtresinden okunur ve kişi o sınıfa atanmış, sınıfın şubesinde hâlâ Öğretmen ve sınıf etkin olmalı;
/// değilse 404 ("var ama sizin değil" sızdırılmaz). Pasif üyelikte oturum zaten doğrulanmaz (401).
/// </summary>
public sealed class AttendanceGuard
{
    private readonly ICurrentSession session;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly IClassroomTeacherRepository teachers;
    private readonly ISender sender;
    private readonly ILocalizationService localization;

    public AttendanceGuard(
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

    /// <summary>Seçili okul, istek sahibi ve Öğretmen rolü olduğu şubeler; öğretmen değilse 403.</summary>
    public async Task<(Guid TenantId, Guid UserId, IReadOnlySet<Guid> TeacherBranchIds)> RequireTeacherAsync(CancellationToken cancellationToken)
    {
        if (this.session.TenantId is { } tenantId && this.session.UserId is { } userId)
        {
            IReadOnlyList<RoleAssignment> roles = await this.roleAssignments.ListInCurrentTenantAsync(userId, cancellationToken).ConfigureAwait(false);
            HashSet<Guid> branchIds = [.. roles.Where(r => r.Role == Role.Teacher && r.BranchId is not null).Select(r => r.BranchId!.Value)];
            if (branchIds.Count > 0)
            {
                return (tenantId, userId, branchIds);
            }
        }

        throw new ForbiddenException(await this.localization.AttendanceTextAsync(AttendanceMessages.TeacherOnly).ConfigureAwait(false));
    }

    /// <summary>Öğretmenin bu sınıfa erişimi: görünür, etkin, atanmış ve şubesinde öğretmen; değilse 404.</summary>
    public async Task<ClassroomSummary> RequireClassroomAsync(Guid userId, IReadOnlySet<Guid> teacherBranchIds, Guid classroomId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teacherBranchIds);
        ClassroomSummary classroom = await this.sender.Send(new GetClassroomByIdQuery(classroomId), cancellationToken).ConfigureAwait(false);
        if (!classroom.IsActive
            || !teacherBranchIds.Contains(classroom.BranchId)
            || !await this.teachers.IsAssignedAsync(classroom.Id, userId, cancellationToken).ConfigureAwait(false))
        {
            throw new NotFoundException(await this.localization.AttendanceTextAsync(AttendanceMessages.ClassroomNotFound).ConfigureAwait(false));
        }

        return classroom;
    }
}
