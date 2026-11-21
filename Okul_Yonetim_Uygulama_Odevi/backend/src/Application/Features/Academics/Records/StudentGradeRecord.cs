namespace OkulYonetim.Application.Features.Academics.Records;

/// <summary>Öğrencinin bir notu (spec 0013 gereksinim 8): ders, tür (<c>Written</c>/<c>Oral</c>), başlık, gün ve puan (boş = girilmedi).</summary>
public sealed record StudentGradeRecord(Guid AssessmentId, string Subject, string Kind, string Title, DateOnly Date, int? Score);
