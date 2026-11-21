using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Features.Academics.Assessments;
using OkulYonetim.Application.Features.Academics.Records;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Academics;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Değerlendirme deposu (spec 0013): değerlendirme kurum + şube filtreli, puan satırları kurum filtreli. Öğretmen adı kişi
/// tablosuyla birleştirilir; listeler ve öğrenci bazlı okuma izlemesiz projeksiyon (T7). Öğrenci bazlı okuma puan satırını
/// değerlendirmeyle birleştirir — değerlendirmenin şube filtresi de uygulanır.
/// </summary>
public sealed class AssessmentRepository : IAssessmentRepository
{
    private readonly OkulDbContext context;

    public AssessmentRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public async Task<(Assessment Assessment, string CreatorName)?> FindWithCreatorAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await this.context.Assessments.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new { Assessment = a, Name = this.context.Users.Where(u => u.Id == a.CreatedByUserId).Select(u => u.FullName).FirstOrDefault() })
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return found is null ? null : (found.Assessment, found.Name ?? string.Empty);
    }

    public async Task<IReadOnlyList<AssessmentRow>> ListForClassroomAsync(Guid classroomId, IReadOnlyCollection<Guid> currentStudentIds, CancellationToken cancellationToken)
    {
        var rows = await this.context.Assessments.AsNoTracking()
            .Where(a => a.ClassroomId == classroomId)
            .OrderByDescending(a => a.Date).ThenByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Select(a => new
            {
                a.Id,
                a.Subject,
                a.Kind,
                a.Title,
                a.Date,
                Name = this.context.Users.Where(u => u.Id == a.CreatedByUserId).Select(u => u.FullName).FirstOrDefault(),
                Graded = this.context.Grades.Count(g => g.AssessmentId == a.Id && currentStudentIds.Contains(g.StudentId) && g.Score != null),
                Total = this.context.Grades.Count(g => g.AssessmentId == a.Id && currentStudentIds.Contains(g.StudentId)),
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(x => new AssessmentRow(x.Id, x.Subject, x.Kind.ToString(), x.Title, x.Date, x.Name ?? string.Empty, x.Graded, x.Total))];
    }

    public async Task<IReadOnlyList<Grade>> ListGradesAsync(Guid assessmentId, bool forUpdate, CancellationToken cancellationToken)
    {
        IQueryable<Grade> query = this.context.Grades.Where(g => g.AssessmentId == assessmentId);
        return await (forUpdate ? query : query.AsNoTracking()).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StudentGradeRecord>> ListForStudentAsync(Guid studentId, DateOnly fromDay, DateOnly toDay, CancellationToken cancellationToken)
    {
        var rows = await this.context.Grades.AsNoTracking()
            .Where(g => g.StudentId == studentId)
            .Join(this.context.Assessments, g => g.AssessmentId, a => a.Id, (g, a) => new { a.Id, a.Subject, a.Kind, a.Title, a.Date, a.CreatedAt, g.Score })
            .Where(x => x.Date >= fromDay && x.Date <= toDay)
            .OrderBy(x => x.Date).ThenBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(x => new StudentGradeRecord(x.Id, x.Subject, x.Kind.ToString(), x.Title, x.Date, x.Score))];
    }

    public void Add(Assessment assessment) => this.context.Assessments.Add(assessment);

    public void AddGrade(Grade grade) => this.context.Grades.Add(grade);
}
