namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Öğrencinin bir aralıktaki yoklama özeti (spec 0014 dayanağı): yoklama alınan gün sayısı, Var / Yok / Geç sayıları ve yok ile
/// geç günleri (artan sırada). Aynı gün iki sınıfta yoklama varsa (gün içinde sınıf değişikliği) o günün son kaydı sayılır.
/// </summary>
public sealed record StudentWeekAttendance(int DaysTaken, int Present, int Absent, int Late, IReadOnlyList<DateOnly> AbsentDays, IReadOnlyList<DateOnly> LateDays)
{
    public static StudentWeekAttendance None { get; } = new(0, 0, 0, 0, [], []);
}
