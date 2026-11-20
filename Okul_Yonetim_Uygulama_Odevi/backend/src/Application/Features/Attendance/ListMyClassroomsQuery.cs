using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Attendance;

/// <summary>Öğretmenin atandığı etkin sınıfları, bugünkü yoklama durumuyla (spec 0010 gereksinim 2). Yalnızca Öğretmen rolü.</summary>
public sealed record ListMyClassroomsQuery : IRequest<IReadOnlyList<MyClassroomRow>>, ILoggableRequest;
