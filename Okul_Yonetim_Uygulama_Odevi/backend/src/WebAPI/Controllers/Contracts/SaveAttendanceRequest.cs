using OkulYonetim.Application.Features.Attendance;

namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Bugünkü yoklama (spec 0010): gün (<c>yyyy-MM-dd</c>, sunucunun Türkiye günüyle aynı olmalı) ve öğrenci durumları.</summary>
public sealed record SaveAttendanceRequest(DateOnly? Day, IReadOnlyList<AttendanceEntryInput>? Entries);
