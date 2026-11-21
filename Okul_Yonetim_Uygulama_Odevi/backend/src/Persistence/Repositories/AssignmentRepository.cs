using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Features.Academics.Assignments;
using OkulYonetim.Application.Features.Academics.Records;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Academics;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Ödev deposu (spec 0013): ödev kurum + şube filtreli, teslim satırları kurum filtreli. Öğretmen adı kişi tablosuyla
/// birleştirilir; listeler ve öğrenci bazlı okuma izlemesiz projeksiyon (T7). Öğrenci bazlı okuma teslim satırını ödevle
/// birleştirir — ödevin şube filtresi de uygulanır.
/// </summary>
public sealed class AssignmentRepository : IAssignmentRepository
{
    private readonly OkulDbContext context;

    public AssignmentRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task<(Assignment Assignment, string CreatorName)?> FindWithCreatorAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await this.context.Assignments.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new { Assignment = a, Name = this.context.Users.Where(u => u.Id == a.CreatedByUserId).Select(u => u.FullName).FirstOrDefault() })
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return found is null ? null : (found.Assignment, found.Name ?? string.Empty);
    }

    public async Task<IReadOnlyList<AssignmentRow>> ListForClassroomAsync(Guid classroomId, IReadOnlyCollection<Guid> currentStudentIds, CancellationToken cancellationToken) =>
        await this.context.Assignments.AsNoTracking()
            .Where(a => a.ClassroomId == classroomId)
            .OrderByDescending(a => a.DueDate).ThenByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Select(a => new AssignmentRow(
                a.Id,
                a.Subject,
                a.Title,
                a.DueDate,
                this.context.Users.Where(u => u.Id == a.CreatedByUserId).Select(u => u.FullName).FirstOrDefault() ?? string.Empty,
                this.context.AssignmentSubmissions.Count(s => s.AssignmentId == a.Id && currentStudentIds.Contains(s.StudentId) && s.Status == SubmissionStatus.Submitted),
                this.context.AssignmentSubmissions.Count(s => s.AssignmentId == a.Id && currentStudentIds.Contains(s.StudentId))))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<AssignmentSubmission>> ListSubmissionsAsync(Guid assignmentId, bool forUpdate, CancellationToken cancellationToken)
    {
        IQueryable<AssignmentSubmission> query = this.context.AssignmentSubmissions.Where(s => s.AssignmentId == assignmentId);
        return await (forUpdate ? query : query.AsNoTracking()).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StudentAssignmentRecord>> ListForStudentAsync(Guid studentId, DateOnly fromDay, DateOnly toDay, CancellationToken cancellationToken)
    {
        var rows = await this.context.AssignmentSubmissions.AsNoTracking()
            .Where(s => s.StudentId == studentId)
            .Join(this.context.Assignments, s => s.AssignmentId, a => a.Id, (s, a) => new { a.Id, a.Subject, a.Title, a.DueDate, a.CreatedAt, s.Status })
            .Where(x => x.DueDate >= fromDay && x.DueDate <= toDay)
            .OrderBy(x => x.DueDate).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(x => new StudentAssignmentRecord(x.Id, x.Subject, x.Title, x.DueDate, x.Status.ToString()))];
    }

    public void Add(Assignment assignment) => this.context.Assignments.Add(assignment);

    public void AddSubmission(AssignmentSubmission submission) => this.context.AssignmentSubmissions.Add(submission);
}
