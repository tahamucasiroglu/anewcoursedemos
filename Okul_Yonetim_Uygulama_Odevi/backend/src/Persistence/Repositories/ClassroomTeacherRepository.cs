using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Features.Administration.ClassroomTeachers;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Sınıf öğretmeni deposu (spec 0010): atama, sınıf, şube, dönem, üyelik ve rol okumaları kendi filtrelerinden geçer
/// (filtre dışı okuma yok). Liste okumaları izlemesiz projeksiyonla (T7).
/// </summary>
public sealed class ClassroomTeacherRepository : IClassroomTeacherRepository
{
    private readonly OkulDbContext context;

    public ClassroomTeacherRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<ClassroomTeacher>> ListForClassroomAsync(Guid classroomId, CancellationToken cancellationToken) =>
        await this.context.ClassroomTeachers.Where(t => t.ClassroomId == classroomId).ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<TeacherOption>> ListAssignedAsync(Guid classroomId, CancellationToken cancellationToken) =>
        await (from t in this.context.ClassroomTeachers.AsNoTracking()
               join u in this.context.Users.AsNoTracking() on t.TeacherUserId equals u.Id
               where t.ClassroomId == classroomId
               select new TeacherOption(u.Id, u.FullName, u.Email))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<TeacherOption>> ListCandidatesAsync(Guid branchId, CancellationToken cancellationToken) =>
        await this.context.Users.AsNoTracking()
            .Where(u => this.context.RoleAssignments.Any(r => r.UserId == u.Id && r.Role == Role.Teacher && r.BranchId == branchId)
                && this.context.Memberships.Any(m => m.UserId == u.Id && m.IsActive))
            .Select(u => new TeacherOption(u.Id, u.FullName, u.Email))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<TeacherClassroomRow>> ListForTeacherAsync(Guid teacherUserId, CancellationToken cancellationToken) =>
        await (from t in this.context.ClassroomTeachers.AsNoTracking()
               join c in this.context.Classrooms.AsNoTracking() on t.ClassroomId equals c.Id
               join b in this.context.Branches.AsNoTracking() on c.BranchId equals b.Id
               join term in this.context.Terms.AsNoTracking() on c.TermId equals term.Id
               where t.TeacherUserId == teacherUserId
               select new TeacherClassroomRow(c.Id, c.Name, b.Id, b.Name, term.Id, term.EndDate, c.IsActive))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<bool> IsAssignedAsync(Guid classroomId, Guid teacherUserId, CancellationToken cancellationToken) =>
        this.context.ClassroomTeachers.AnyAsync(t => t.ClassroomId == classroomId && t.TeacherUserId == teacherUserId, cancellationToken);

    public async Task<IReadOnlyList<(Guid ClassroomId, string FullName)>> ListAssignedNamesAsync(IReadOnlyCollection<Guid> classroomIds, CancellationToken cancellationToken) =>
        [.. (await (from t in this.context.ClassroomTeachers.AsNoTracking()
                    join u in this.context.Users.AsNoTracking() on t.TeacherUserId equals u.Id
                    where classroomIds.Contains(t.ClassroomId)
                    select new { t.ClassroomId, u.FullName })
                .ToListAsync(cancellationToken).ConfigureAwait(false))
            .Select(x => (x.ClassroomId, x.FullName))];

    public void Add(ClassroomTeacher assignment) => this.context.ClassroomTeachers.Add(assignment);

    public void Remove(ClassroomTeacher assignment) => this.context.ClassroomTeachers.Remove(assignment);
}
