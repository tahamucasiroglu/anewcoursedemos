using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Students;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Öğrenci deposu (spec 0009): her okuma kurum + şube filtresinden geçer (öğrenci şubesinin görünürlüğünü miras alır);
/// filtre dışı okuma yok. Liste satırları izlemesiz projeksiyonla (T7); sağlık alanları listeye hiç okunmaz.
/// </summary>
public sealed class StudentRepository : IStudentRepository
{
    private readonly OkulDbContext context;

    public StudentRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<Student?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.Students.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<StudentRow>> ListRowsAsync(CancellationToken cancellationToken) =>
        await (from s in this.context.Students.AsNoTracking()
               join b in this.context.Branches on s.BranchId equals b.Id
               join c in this.context.Classrooms on s.ClassroomId equals c.Id into classes
               from c in classes.DefaultIfEmpty()
               select new StudentRow(
                   s.Id,
                   s.FullName,
                   s.SchoolNumber,
                   s.BranchId,
                   b.Name,
                   s.ClassroomId,
                   c == null ? null : c.Name,
                   this.context.StudentGuardians.Count(g => g.StudentId == s.Id),
                   s.IsActive))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<bool> SchoolNumberExistsAsync(string schoolNumberKey, Guid? exceptId, CancellationToken cancellationToken) =>
        this.context.Students.AnyAsync(s => s.SchoolNumberKey == schoolNumberKey && s.Id != exceptId, cancellationToken);

    public async Task<IReadOnlyList<RosterStudent>> ListClassroomRosterAsync(Guid classroomId, CancellationToken cancellationToken) =>
        await this.context.Students.AsNoTracking()
            .Where(s => s.ClassroomId == classroomId && s.IsActive)
            .Select(s => new RosterStudent(s.Id, s.FullName))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<StudentBrief>> ListBriefsAsync(Guid? branchId, Guid? classroomId, IReadOnlyCollection<Guid>? ids, CancellationToken cancellationToken)
    {
        IQueryable<Student> query = this.context.Students.AsNoTracking().Where(s => s.IsActive);
        if (branchId is { } branch)
        {
            query = query.Where(s => s.BranchId == branch);
        }

        if (classroomId is { } classroom)
        {
            query = query.Where(s => s.ClassroomId == classroom);
        }

        if (ids is not null)
        {
            query = query.Where(s => ids.Contains(s.Id));
        }

        return await query.Select(s => new StudentBrief(s.Id, s.FullName, s.BranchId, s.ClassroomId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StudentLabel>> ListLabelsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await (from s in this.context.Students.AsNoTracking()
               join b in this.context.Branches on s.BranchId equals b.Id
               join c in this.context.Classrooms on s.ClassroomId equals c.Id into classes
               from c in classes.DefaultIfEmpty()
               where ids.Contains(s.Id)
               select new StudentLabel(s.Id, s.FullName, s.BranchId, b.Name, c == null ? null : c.Name, s.IsActive, s.SchoolNumber))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<PickupPerson>> ListPickupPersonsAsync(Guid studentId, CancellationToken cancellationToken) =>
        await this.context.PickupPersons.Where(p => p.StudentId == studentId).OrderBy(p => p.Position)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public void Add(Student student) => this.context.Students.Add(student);

    public void AddPickupPerson(PickupPerson person) => this.context.PickupPersons.Add(person);

    public void RemovePickupPerson(PickupPerson person) => this.context.PickupPersons.Remove(person);
}
