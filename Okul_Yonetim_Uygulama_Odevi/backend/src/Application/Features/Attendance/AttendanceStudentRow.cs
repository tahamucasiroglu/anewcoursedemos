namespace OkulYonetim.Application.Features.Attendance;

/// <summary>Yoklama listesindeki öğrenci ve durumu (<c>Present</c>, <c>Absent</c>, <c>Late</c>; kayıt yoksa <c>Present</c>).</summary>
public sealed record AttendanceStudentRow(Guid StudentId, string FullName, string Status);
