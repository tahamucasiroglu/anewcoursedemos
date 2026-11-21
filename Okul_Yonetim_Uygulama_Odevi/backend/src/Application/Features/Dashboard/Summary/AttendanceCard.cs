using OkulYonetim.Application.Features.Attendance;

namespace OkulYonetim.Application.Features.Dashboard.Summary;

/// <summary>
/// Devamsızlık kartı (spec 0017 gereksinim 3, C-3): bugün (Türkiye günü), okul günü mü, sayılan sınıf (güncel dönemde etkin ve öğrencisi
/// olan), yoklaması alınan ve alınmayan sayısı, alınmayanların listesi (şube, sınıf, atanmış öğretmen), yok ve geç öğrenci sayısı, yok
/// öğrencilerin listesi (ad-soyad, şube, sınıf).
/// </summary>
public sealed record AttendanceCard(
    DateOnly Today,
    bool IsSchoolDay,
    int ClassroomCount,
    int TakenCount,
    int NotTakenCount,
    IReadOnlyList<DayClassroomRow> NotTaken,
    int AbsentCount,
    int LateCount,
    IReadOnlyList<AbsentStudentRow> AbsentStudents);
