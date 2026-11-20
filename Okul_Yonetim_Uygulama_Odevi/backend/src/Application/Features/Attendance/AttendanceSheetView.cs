namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Bugünkü yoklama ekranı (spec 0010 gereksinim 3, 5): sınıf, şube, gün (Türkiye günü), okul günü mü, kaydedildi mi ve
/// kayıt saati (Türkiye saatiyle <c>SS:DD</c>), öğrenciler Türkçe ad sırasıyla. Okul günü değilse öğrenci listesi boştur.
/// <c>Version</c> yoklamanın sürümüdür (henüz kaydedilmediyse 0); kaydetmede aynen geri gönderilir (0010 review F-1).
/// </summary>
public sealed record AttendanceSheetView(
    Guid ClassroomId,
    string ClassroomName,
    string BranchName,
    DateOnly Day,
    bool IsSchoolDay,
    bool IsSaved,
    string? SavedTime,
    IReadOnlyList<AttendanceStudentRow> Students,
    int Version);
