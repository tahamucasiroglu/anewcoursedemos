using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>Sınıfın ödevleri (spec 0013 gereksinim 3): yalnızca sınıfa atanmış öğretmen; istek kimlik dışında veri taşımaz.</summary>
public sealed record ListClassroomAssignmentsQuery(Guid ClassroomId) : IRequest<IReadOnlyList<AssignmentRow>>, ILoggableRequest;
