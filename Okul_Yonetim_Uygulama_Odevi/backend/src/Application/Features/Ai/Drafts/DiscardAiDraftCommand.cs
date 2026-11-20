using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Ai.Drafts;

/// <summary>"Vazgeç" (spec 0012 gereksinim 2, AC-3): bekleyen taslak (döküm dahil) silinir. Yalnızca oluşturan kişi; yoksa, başkasının ya da kullanılmışsa 404. Yalnızca kimlik taşır, loglanabilir.</summary>
public sealed record DiscardAiDraftCommand(Guid Id) : IRequest<bool>, ILoggableRequest;
