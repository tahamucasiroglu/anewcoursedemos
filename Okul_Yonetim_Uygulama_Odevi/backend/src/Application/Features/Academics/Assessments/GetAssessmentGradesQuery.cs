using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Academics.Assessments;

/// <summary>Değerlendirmenin puan listesi (spec 0013 gereksinim 6): sınıfa atanmış her öğretmen görür, yalnızca açan değiştirebilir.</summary>
public sealed record GetAssessmentGradesQuery(Guid AssessmentId) : IRequest<AssessmentGradesView>, ILoggableRequest;
