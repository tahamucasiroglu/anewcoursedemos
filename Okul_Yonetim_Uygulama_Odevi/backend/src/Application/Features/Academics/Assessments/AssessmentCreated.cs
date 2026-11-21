namespace OkulYonetim.Application.Features.Academics.Assessments;

/// <summary>Açılan değerlendirme (spec 0013 gereksinim 5): kimlik ve kapsadığı öğrenci sayısı.</summary>
public sealed record AssessmentCreated(Guid Id, int StudentCount);
