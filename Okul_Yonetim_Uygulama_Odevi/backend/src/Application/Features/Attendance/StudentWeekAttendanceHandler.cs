using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Attendance;

namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Öğrenci bazlı yoklama okuması (spec 0014; <see cref="GetStudentWeekAttendanceQuery"/>): yalnızca bu öğrencinin satırları, günü
/// aralıkta olan yoklamalardan; gün başına son kayıt. Erişim denetimi çağırandadır. Ters aralık boş döner.
/// </summary>
public sealed class StudentWeekAttendanceHandler : IRequestHandler<GetStudentWeekAttendanceQuery, StudentWeekAttendance>
{
    private readonly IAttendanceRepository attendance;

    public StudentWeekAttendanceHandler(IAttendanceRepository attendance)
    {
        this.attendance = attendance;
    }

    public async Task<StudentWeekAttendance> Handle(GetStudentWeekAttendanceQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.From > request.To)
        {
            return StudentWeekAttendance.None;
        }

        IReadOnlyList<(DateOnly Day, AttendanceStatus Status)> days =
            await this.attendance.ListStudentDaysAsync(request.StudentId, request.From, request.To, cancellationToken).ConfigureAwait(false);
        List<(DateOnly Day, AttendanceStatus Status)> ordered = [.. days.OrderBy(d => d.Day)];
        return new StudentWeekAttendance(
            ordered.Count,
            ordered.Count(d => d.Status == AttendanceStatus.Present),
            ordered.Count(d => d.Status == AttendanceStatus.Absent),
            ordered.Count(d => d.Status == AttendanceStatus.Late),
            [.. ordered.Where(d => d.Status == AttendanceStatus.Absent).Select(d => d.Day)],
            [.. ordered.Where(d => d.Status == AttendanceStatus.Late).Select(d => d.Day)]);
    }
}
