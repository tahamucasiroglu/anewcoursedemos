using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>"Vazgeç" (spec 0014 gereksinim 5, AC-7): öğrenci-haftanın bekleyen özet taslağı (dayanağıyla) silinir; yoksa ya da gönderilmişse 404. Yalnızca kimlik taşır, loglanabilir.</summary>
public sealed record DiscardWeeklySummaryDraftCommand(Guid StudentId, string? Week) : IRequest<bool>, ILoggableRequest;
