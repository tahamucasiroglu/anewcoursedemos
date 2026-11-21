using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Academics.Records;
using OkulYonetim.Application.Features.Attendance;
using OkulYonetim.Domain.Ai;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>
/// Haftalık özetin dayanağını toplar (spec 0014 gereksinim 2): modüller arası okuma sorgularıyla — Attendance
/// <see cref="GetStudentWeekAttendanceQuery"/> ve Academics <see cref="GetStudentAcademicRecordQuery"/> — haftanın Pazartesi'si ile
/// Pazar'ı arasında (uçlar dahil), yalnızca verilen öğrenci için. <b>İki sorgu da erişim denetlemez</b> ve okul + şube görünürlüğü olan
/// oturum ister (plan 0014 Risks 9): bu toplayıcıyı yalnızca <see cref="WeeklySummaryGuard.RequireStudentAsync"/>'ten geçmiş
/// işleyiciler çağırır — öğrenci, guard'ın döndürdüğü öğrencidir.
/// </summary>
public sealed class WeeklySummarySourceCollector
{
    private readonly ISender sender;

    public WeeklySummarySourceCollector(ISender sender)
    {
        this.sender = sender;
    }

    public async Task<WeeklySummarySource> CollectAsync(WeeklySummaryStudent student, SchoolWeek week, string? teacherNote, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(student);
        StudentWeekAttendance attendance = await this.sender.Send(new GetStudentWeekAttendanceQuery(student.StudentId, week.Start, week.End), cancellationToken).ConfigureAwait(false);
        StudentAcademicRecord record = await this.sender.Send(new GetStudentAcademicRecordQuery(student.StudentId, week.Start, week.End), cancellationToken).ConfigureAwait(false);
        string? note = string.IsNullOrWhiteSpace(teacherNote) ? null : teacherNote.Trim();
        return new WeeklySummarySource(
            WeeklySummarySource.FirstNameOf(student.FullName),
            week.Start,
            attendance,
            [.. record.Assignments.OrderBy(a => a.DueDate).ThenBy(a => a.AssignmentId)],
            [.. record.Grades.OrderBy(g => g.Date).ThenBy(g => g.AssessmentId)],
            note);
    }
}
