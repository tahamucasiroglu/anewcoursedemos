using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Organization.Queries.GetSchoolCalendar;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListMyChildren;
using OkulYonetim.Domain.Academics;

namespace OkulYonetim.Application.Features.Academics.Records;

/// <summary>
/// Velinin çocuk özeti (spec 0013 C-7, gereksinim 7). Yetki Students'ın "Çocuklarım" sorgusundan: veli rolü yoksa 403; öğrenci
/// kişinin <b>bağlı</b> ve ayrılmamış çocukları arasında değilse 404 ("Bu öğrenci artık görüntülenemiyor.") — veli rolü şube
/// düzeyinde olduğu için şube filtresi tek başına yetmez, farklı şubedeki kardeş de bağ üzerinden açılır (BR-2). Aralık bugünden
/// (Türkiye günü) ve etkin dönemden (<see cref="GetSchoolCalendarQuery"/>: güncel ya da yaklaşan) kurulur, veri öğrenci bazlı
/// okumadan (<see cref="GetStudentAcademicRecordQuery"/>) gelir ve üç bölüme ayrılır: bekleyen (teslim günü bugün ya da sonra,
/// en yakın üstte), son 30 gün (teslim günü dün ile 30 gün önce arası, yeniden eskiye) ve etkin dönemin notları (yeniden eskiye;
/// dönem yoksa boş).
/// </summary>
public sealed class MyChildAcademicsHandler : IRequestHandler<GetMyChildAcademicsQuery, ChildAcademicsView>
{
    public const int RecentDays = 30;

    private readonly ISender sender;
    private readonly SchoolCalendar calendar;
    private readonly AcademicsGuard guard;

    public MyChildAcademicsHandler(ISender sender, SchoolCalendar calendar, AcademicsGuard guard)
    {
        this.sender = sender;
        this.calendar = calendar;
        this.guard = guard;
    }

    public async Task<ChildAcademicsView> Handle(GetMyChildAcademicsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        IReadOnlyList<ChildRow> children = await this.sender.Send(new ListMyChildrenQuery(), cancellationToken).ConfigureAwait(false);
        ChildRow child = children.FirstOrDefault(c => c.Id == request.StudentId)
            ?? throw await this.guard.NotFoundAsync(AcademicsMessages.ChildNotFound).ConfigureAwait(false);

        DateOnly today = this.calendar.Today();
        DateOnly recentFrom = today.AddDays(-RecentDays);
        DateOnly from = recentFrom;
        DateOnly to = today.AddDays(Assignment.DueMaxDays);
        SchoolCalendarView schoolCalendar = await this.sender.Send(new GetSchoolCalendarQuery(), cancellationToken).ConfigureAwait(false);
        if (schoolCalendar.Term is { } term)
        {
            from = term.StartDate < from ? term.StartDate : from;
            to = term.EndDate > to ? term.EndDate : to;
        }

        StudentAcademicRecord record = await this.sender.Send(new GetStudentAcademicRecordQuery(child.Id, from, to), cancellationToken).ConfigureAwait(false);
        return new ChildAcademicsView(
            child.Id,
            child.FullName,
            today,
            [.. record.Assignments.Where(a => a.DueDate >= today).OrderBy(a => a.DueDate).ThenBy(a => a.AssignmentId)],
            [.. record.Assignments.Where(a => a.DueDate < today && a.DueDate >= recentFrom).OrderByDescending(a => a.DueDate).ThenByDescending(a => a.AssignmentId)],
            schoolCalendar.Term is { } current
                ? [.. record.Grades.Where(g => g.Date >= current.StartDate && g.Date <= current.EndDate).OrderByDescending(g => g.Date).ThenByDescending(g => g.AssessmentId)]
                : []);
    }
}
