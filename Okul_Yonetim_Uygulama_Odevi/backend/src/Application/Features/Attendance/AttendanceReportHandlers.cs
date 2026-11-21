using System.Globalization;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Features.Organization.Queries.ListTodayClassrooms;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Features.Students.Queries.ListStudentBriefs;
using OkulYonetim.Application.Features.Students.Queries.ListStudentLabels;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Attendance;

namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Yönetici panelinin yoklama okumaları (spec 0017; uçsuz, erişimi çağıran denetler). Günlük özet: Organization'ın bugünkü sınıfları
/// (okul günü değilse boş), Students'ın sınıflardaki ayrılmamış öğrenci sayıları (öğrencisiz sınıf sayılmaz), bugünkü yoklamalar ve
/// "Var" olmayan satırlar; "Yok" öğrencilerin adı Students etiketinden, şube/sınıf yoklamanın alındığı sınıftan. Rapor: aralıktaki "Yok"/"Geç"
/// satırları öğrenci + gün başına sayılır (aynı gün iki sınıfta kayıt tek gün). Sorgular gruplu ve izlemesiz (T7).
/// </summary>
public sealed class AttendanceReportHandlers :
    IRequestHandler<GetAttendanceDaySummaryQuery, AttendanceDaySummary>,
    IRequestHandler<ListAttendanceTotalsQuery, IReadOnlyList<StudentAttendanceTotal>>
{
    private static readonly StringComparer TurkishOrder = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: true);
    private readonly IAttendanceRepository attendance;
    private readonly ISender sender;

    public AttendanceReportHandlers(IAttendanceRepository attendance, ISender sender)
    {
        this.attendance = attendance;
        this.sender = sender;
    }

    public async Task<AttendanceDaySummary> Handle(GetAttendanceDaySummaryQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        TodayClassrooms today = await this.sender.Send(new ListTodayClassroomsQuery(request.BranchId), cancellationToken).ConfigureAwait(false);
        if (!today.IsSchoolDay)
        {
            return new AttendanceDaySummary(today.Today, false, [], [], 0, 0);
        }

        Dictionary<Guid, int> studentCounts = (await this.sender.Send(new ListStudentBriefsQuery(BranchId: request.BranchId), cancellationToken).ConfigureAwait(false))
            .Where(s => s.ClassroomId is not null)
            .GroupBy(s => s.ClassroomId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
        List<TodayClassroom> eligible = [.. today.Classrooms.Where(c => studentCounts.ContainsKey(c.Id))];
        if (eligible.Count == 0)
        {
            return new AttendanceDaySummary(today.Today, true, [], [], 0, 0);
        }

        List<Guid> ids = [.. eligible.Select(c => c.Id)];
        HashSet<Guid> taken = [.. await this.attendance.ListTakenClassroomIdsAsync(ids, today.Today, cancellationToken).ConfigureAwait(false)];
        IReadOnlyList<(Guid ClassroomId, Guid StudentId, AttendanceStatus Status)> exceptions =
            await this.attendance.ListDayExceptionsAsync(ids, today.Today, cancellationToken).ConfigureAwait(false);
        Dictionary<Guid, int> order = eligible.Select((c, i) => (c.Id, i)).ToDictionary(x => x.Id, x => x.i);
        Dictionary<Guid, TodayClassroom> byId = eligible.ToDictionary(c => c.Id);
        Dictionary<Guid, (Guid ClassroomId, Guid StudentId, AttendanceStatus Status)> absent = exceptions
            .Where(e => e.Status == AttendanceStatus.Absent)
            .GroupBy(e => e.StudentId)
            .ToDictionary(g => g.Key, g => g.First());
        Dictionary<Guid, StudentLabel> labels = absent.Count == 0
            ? []
            : (await this.sender.Send(new ListStudentLabelsQuery([.. absent.Keys]), cancellationToken).ConfigureAwait(false)).ToDictionary(l => l.Id);
        List<AbsentStudentRow> absentRows = [.. absent.Values
            .Where(a => labels.ContainsKey(a.StudentId))
            .OrderBy(a => order[a.ClassroomId])
            .ThenBy(a => labels[a.StudentId].FullName, TurkishOrder)
            .ThenBy(a => a.StudentId)
            .Select(a => new AbsentStudentRow(a.StudentId, labels[a.StudentId].FullName, byId[a.ClassroomId].BranchName, byId[a.ClassroomId].Name))];
        return new AttendanceDaySummary(
            today.Today,
            true,
            [.. eligible.Select(c => new DayClassroomRow(c.Id, c.BranchName, c.Name, c.TeacherNames, studentCounts[c.Id], taken.Contains(c.Id)))],
            absentRows,
            absentRows.Count,
            exceptions.Where(e => e.Status == AttendanceStatus.Late).Select(e => e.StudentId).Distinct().Count());
    }

    public async Task<IReadOnlyList<StudentAttendanceTotal>> Handle(ListAttendanceTotalsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        IReadOnlyList<(Guid StudentId, DateOnly Day, AttendanceStatus Status)> marks =
            await this.attendance.ListStatusDaysAsync(request.FromDay, request.ToDay, request.BranchId, request.ClassroomId, cancellationToken).ConfigureAwait(false);
        var counts = marks
            .GroupBy(m => m.StudentId)
            .Select(g => (
                StudentId: g.Key,
                Absent: g.Where(m => m.Status == AttendanceStatus.Absent).Select(m => m.Day).Distinct().Count(),
                Late: g.Where(m => m.Status == AttendanceStatus.Late).Select(m => m.Day).Distinct().Count()))
            .Where(x => x.Absent >= request.MinAbsent)
            .ToList();
        if (counts.Count == 0)
        {
            return [];
        }

        Dictionary<Guid, StudentLabel> labels = (await this.sender.Send(new ListStudentLabelsQuery([.. counts.Select(c => c.StudentId)]), cancellationToken).ConfigureAwait(false))
            .ToDictionary(l => l.Id);
        return [.. counts
            .Where(c => labels.ContainsKey(c.StudentId))
            .Select(c => (Count: c, Label: labels[c.StudentId]))
            .OrderByDescending(x => x.Count.Absent)
            .ThenBy(x => x.Label.FullName, TurkishOrder)
            .ThenBy(x => x.Label.Id)
            .Select(x => new StudentAttendanceTotal(x.Label.Id, x.Label.FullName, x.Label.SchoolNumber, x.Label.BranchName, x.Label.ClassroomName, x.Count.Absent, x.Count.Late))];
    }
}
