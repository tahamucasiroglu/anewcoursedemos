namespace OkulYonetim.Application.Features.Academics.Records;

/// <summary>Öğrencinin bir tarih aralığındaki ödevleri (teslim gününe göre) ve notları (değerlendirme gününe göre) — spec 0013 gereksinim 8; spec 0014 ve veli özeti okur.</summary>
public sealed record StudentAcademicRecord(IReadOnlyList<StudentAssignmentRecord> Assignments, IReadOnlyList<StudentGradeRecord> Grades);
