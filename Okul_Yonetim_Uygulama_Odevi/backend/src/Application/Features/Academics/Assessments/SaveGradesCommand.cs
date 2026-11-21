using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Academics.Assessments;

/// <summary>
/// Puanları tek seferde kaydeder, düzeltir ya da siler (spec 0013 gereksinim 6); bir puan geçersizse hiçbiri kaydedilmez.
/// Gönderilmeyen öğrencinin puanı değişmez. Puan taşıdığı için bütünüyle loglanmaz (T9).
/// </summary>
public sealed record SaveGradesCommand(Guid AssessmentId, IReadOnlyList<GradeInput>? Grades) : IRequest<GradesSaveResult>;
