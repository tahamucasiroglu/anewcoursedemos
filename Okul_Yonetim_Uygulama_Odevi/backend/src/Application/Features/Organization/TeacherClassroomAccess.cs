using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization.Queries.GetClassroomById;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Organization;

/// <summary>
/// Öğretmen–sınıf ilişkisinin tek kaynağı (spec 0010 sınıf öğretmeni bağı; plan 0013 Risks 2): yoklama (<c>AttendanceGuard</c>)
/// ve ödev/not (<c>AcademicsGuard</c>) aynı kuralı buradan kullanır. Her istekte veritabanından: kişinin seçili okulda Öğretmen
/// rolü olduğu şubeler (<see cref="TeacherAsync"/>; yoksa <c>null</c> — çağıran 403 verir) ve sınıfa erişim
/// (<see cref="AssignedClassroomAsync"/>): sınıf kurum + şube filtresinden okunur (görünmeyen sınıf "bulunamadı"), etkin olmalı,
/// sınıfın şubesinde kişi hâlâ Öğretmen ve sınıfa atanmış olmalı; değilse <c>null</c> — çağıran 404 verir.
/// </summary>
public sealed class TeacherClassroomAccess
{
    private readonly ICurrentSession session;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly IClassroomTeacherRepository teachers;
    private readonly ISender sender;

    public TeacherClassroomAccess(ICurrentSession session, IRoleAssignmentRepository roleAssignments, IClassroomTeacherRepository teachers, ISender sender)
    {
        this.session = session;
        this.roleAssignments = roleAssignments;
        this.teachers = teachers;
        this.sender = sender;
    }

    /// <summary>Seçili okul, istek sahibi ve Öğretmen rolü olduğu şubeler; öğretmen değilse <c>null</c>.</summary>
    public async Task<(Guid TenantId, Guid UserId, IReadOnlySet<Guid> TeacherBranchIds)?> TeacherAsync(CancellationToken cancellationToken)
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

        return null;
    }

    /// <summary>
    /// Öğretmenin bu sınıfa erişimi: görünür, etkin, atanmış ve şubesinde öğretmen; değilse <c>null</c>. Görünmeyen sınıfta
    /// sınıf sorgusu kendi "bulunamadı" hatasını (404) fırlatır.
    /// </summary>
    public async Task<ClassroomSummary?> AssignedClassroomAsync(Guid userId, IReadOnlySet<Guid> teacherBranchIds, Guid classroomId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(teacherBranchIds);
        ClassroomSummary classroom = await this.sender.Send(new GetClassroomByIdQuery(classroomId), cancellationToken).ConfigureAwait(false);
        return classroom.IsActive
            && teacherBranchIds.Contains(classroom.BranchId)
            && await this.teachers.IsAssignedAsync(classroom.Id, userId, cancellationToken).ConfigureAwait(false)
            ? classroom
            : null;
    }
}
