namespace OkulYonetim.Application.Features.Academics.Assessments;

/// <summary>
/// Puan listesi (spec 0013 gereksinim 6): değerlendirmenin ders, tür, başlık ve günü, açan öğretmen, isteyen değiştirebilir mi
/// (<c>CanEdit</c> — yalnızca açan) ve sınıfta süren öğrencileri Türkçe ad sırasıyla puanlarıyla (boş = girilmedi).
/// </summary>
public sealed record AssessmentGradesView(Guid AssessmentId, string Subject, string Kind, string Title, DateOnly Date, string CreatedByName, bool CanEdit, IReadOnlyList<GradeStudentRow> Students);
