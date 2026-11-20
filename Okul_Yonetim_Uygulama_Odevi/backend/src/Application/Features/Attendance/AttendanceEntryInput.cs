namespace OkulYonetim.Application.Features.Attendance;

/// <summary>Kaydedilen yoklamada bir öğrencinin durumu: <c>Present</c>, <c>Absent</c> ya da <c>Late</c>.</summary>
public sealed record AttendanceEntryInput(Guid StudentId, string? Status);
