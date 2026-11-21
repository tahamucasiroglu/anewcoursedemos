using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>Öğretmenin haftalık özet için sınıfları (spec 0014 gereksinim 1): yalnızca atandığı etkin sınıflar; öğretmen değilse 403. Alan taşımaz, loglanabilir.</summary>
public sealed record ListWeeklySummaryClassroomsQuery : IRequest<IReadOnlyList<WeeklySummaryClassroom>>, ILoggableRequest;
