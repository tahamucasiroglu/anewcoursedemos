using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Academics.Assessments;

/// <summary>
/// Sınıf için değerlendirme açar (spec 0013 gereksinim 5): ders adı, tür (<c>Written</c>/<c>Oral</c>), başlık ve gün. Bütünüyle
/// loglanmaz (T9); işleyici yalnızca kimlik ve sayıları loglar.
/// </summary>
public sealed record CreateAssessmentCommand(Guid ClassroomId, string? Subject, string? Kind, string? Title, DateOnly? Date) : IRequest<AssessmentCreated>;
