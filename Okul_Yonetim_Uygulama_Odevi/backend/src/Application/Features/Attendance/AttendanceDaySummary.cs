namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Bugünün yoklama özeti (spec 0017 gereksinim 3): okulun bugünü (Türkiye günü), okul günü mü, güncel dönemde etkin ve en az bir öğrencisi
/// olan sınıflar (alındı/alınmadı), "Yok" öğrenciler ve yok/geç öğrenci sayıları. Okul günü değilse listeler boş.
/// </summary>
public sealed record AttendanceDaySummary(DateOnly Today, bool IsSchoolDay, IReadOnlyList<DayClassroomRow> Classrooms, IReadOnlyList<AbsentStudentRow> AbsentStudents, int AbsentCount, int LateCount);
