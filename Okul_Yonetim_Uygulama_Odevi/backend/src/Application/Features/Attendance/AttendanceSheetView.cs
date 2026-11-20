namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Bugünkü yoklama ekranı (spec 0010 gereksinim 3, 5): sınıf, şube, gün (Türkiye günü), okul günü mü, kaydedildi mi ve
/// kayıt saati (Türkiye saatiyle <c>SS:DD</c>), öğrenciler Türkçe ad sırasıyla. Okul günü değilse öğrenci listesi boştur.
/// </summary>
public sealed record AttendanceSheetView(
    Guid ClassroomId,
    string ClassroomName,
    string BranchName,
    DateOnly Day,
    bool IsSchoolDay,
    bool IsSaved,
    string? SavedTime,
    IReadOnlyList<AttendanceStudentRow> Students);
