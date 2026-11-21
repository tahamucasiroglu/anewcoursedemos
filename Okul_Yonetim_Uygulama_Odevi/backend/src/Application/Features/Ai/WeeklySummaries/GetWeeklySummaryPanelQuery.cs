using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>Öğrenci + hafta → dayanak ve özet durumu (spec 0014 gereksinim 2, AC-2). İstek yalnızca kimlik taşır, loglanabilir (yanıt loglanmaz).</summary>
public sealed record GetWeeklySummaryPanelQuery(Guid StudentId, string? Week) : IRequest<WeeklySummaryPanel>, ILoggableRequest;
