using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>Ödevin teslim listesi (spec 0013 gereksinim 4): sınıfa atanmış her öğretmen görür, yalnızca veren değiştirebilir.</summary>
public sealed record GetAssignmentSubmissionsQuery(Guid AssignmentId) : IRequest<AssignmentSubmissionsView>, ILoggableRequest;
