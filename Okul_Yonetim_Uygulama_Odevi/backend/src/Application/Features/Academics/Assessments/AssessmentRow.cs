namespace OkulYonetim.Application.Features.Academics.Assessments;

/// <summary>Öğretmenin değerlendirme listesindeki satır (spec 0013 gereksinim 6): ders, tür (<c>Written</c>/<c>Oral</c>), başlık, gün, açan öğretmen ve "x / y puan girildi" sayıları (yalnızca sınıfta süren öğrenciler).</summary>
public sealed record AssessmentRow(Guid Id, string Subject, string Kind, string Title, DateOnly Date, string CreatedByName, int GradedCount, int StudentCount);
