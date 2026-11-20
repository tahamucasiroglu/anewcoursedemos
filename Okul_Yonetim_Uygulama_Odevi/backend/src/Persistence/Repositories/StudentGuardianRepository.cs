using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Students;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Veli-öğrenci bağı deposu (spec 0009): bağlar kurum filtreli; öğrenci, şube ve sınıf kendi filtrelerinden geçerek
/// birleştirilir (filtre dışı okuma yok). Velinin çocuk listesi yalnızca kendi bağlarından ve "ayrıldı" olmayan
/// öğrencilerden kurulur (ilişki duvarı).
/// </summary>
public sealed class StudentGuardianRepository : IStudentGuardianRepository
{
    private readonly OkulDbContext context;

    public StudentGuardianRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<StudentGuardian?> FindAsync(Guid studentId, Guid guardianUserId, CancellationToken cancellationToken) =>
        this.context.StudentGuardians.FirstOrDefaultAsync(g => g.StudentId == studentId && g.GuardianUserId == guardianUserId, cancellationToken);

    public Task<int> CountForStudentAsync(Guid studentId, CancellationToken cancellationToken) =>
        this.context.StudentGuardians.CountAsync(g => g.StudentId == studentId, cancellationToken);

    public async Task<IReadOnlyList<StudentGuardianView>> ListGuardiansAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var rows = await (from g in this.context.StudentGuardians.AsNoTracking()
                          join u in this.context.Users.AsNoTracking() on g.GuardianUserId equals u.Id
                          where g.StudentId == studentId
                          orderby g.CreatedAt, g.Id
                          select new { u.Id, u.FullName, u.Email, u.Phone, g.Relationship })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new StudentGuardianView(r.Id, r.FullName, r.Email, r.Phone, r.Relationship.ToString()))];
    }

    public async Task<IReadOnlyList<SiblingRow>> ListSiblingsAsync(Guid studentId, CancellationToken cancellationToken)
    {
        IQueryable<Guid> guardians = this.context.StudentGuardians.Where(g => g.StudentId == studentId).Select(g => g.GuardianUserId);
        IQueryable<Guid> siblingIds = this.context.StudentGuardians
            .Where(g => g.StudentId != studentId && guardians.Contains(g.GuardianUserId))
            .Select(g => g.StudentId)
            .Distinct();
        return await (from s in this.context.Students.AsNoTracking()
                      join b in this.context.Branches on s.BranchId equals b.Id
                      join c in this.context.Classrooms on s.ClassroomId equals c.Id into classes
                      from c in classes.DefaultIfEmpty()
                      where siblingIds.Contains(s.Id)
                      select new SiblingRow(s.Id, s.FullName, b.Name, c == null ? null : c.Name, s.IsActive))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ChildRow>> ListChildrenAsync(Guid guardianUserId, CancellationToken cancellationToken) =>
        await (from g in this.context.StudentGuardians.AsNoTracking()
               join s in this.context.Students on g.StudentId equals s.Id
               join b in this.context.Branches on s.BranchId equals b.Id
               join c in this.context.Classrooms on s.ClassroomId equals c.Id into classes
               from c in classes.DefaultIfEmpty()
               where g.GuardianUserId == guardianUserId && s.IsActive
               select new ChildRow(s.Id, s.FullName, b.Name, c == null ? null : c.Name))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public Task<bool> IsLinkedAsync(Guid studentId, Guid guardianUserId, CancellationToken cancellationToken) =>
        this.context.StudentGuardians.AnyAsync(g => g.StudentId == studentId && g.GuardianUserId == guardianUserId, cancellationToken);

    public void Add(StudentGuardian link) => this.context.StudentGuardians.Add(link);

    public void Remove(StudentGuardian link) => this.context.StudentGuardians.Remove(link);
}
