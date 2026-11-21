using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Academics.Assessments;

/// <summary>Sınıfın değerlendirmeleri (spec 0013 gereksinim 6): yalnızca sınıfa atanmış öğretmen; istek kimlik dışında veri taşımaz.</summary>
public sealed record ListClassroomAssessmentsQuery(Guid ClassroomId) : IRequest<IReadOnlyList<AssessmentRow>>, ILoggableRequest;
