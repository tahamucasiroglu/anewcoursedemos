using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>Sınıf + hafta ("current"/"previous"; boşsa bu hafta) → öğrenciler ve özet durumları (spec 0014 gereksinim 1, AC-1). Yalnızca kimlik taşır, loglanabilir.</summary>
public sealed record ListWeeklySummaryStatusQuery(Guid ClassroomId, string? Week) : IRequest<WeeklySummaryList>, ILoggableRequest;
