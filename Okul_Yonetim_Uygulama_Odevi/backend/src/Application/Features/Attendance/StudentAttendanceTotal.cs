namespace OkulYonetim.Application.Features.Attendance;

/// <summary>Öğrencinin aralıktaki yok ve geç gün sayısı (spec 0017 devamsızlık raporu): ad-soyad, okul no, bugünkü şube ve sınıf etiketiyle.</summary>
public sealed record StudentAttendanceTotal(Guid StudentId, string FullName, string? SchoolNumber, string BranchName, string? ClassroomName, int AbsentDays, int LateDays);
