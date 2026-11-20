using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Velinin onay cevabı (spec 0011 gereksinim 4): duyuru, öğrenci ve cevap (<c>Approved</c>/<c>Declined</c>). Yalnızca kimlik ve cevap taşır.</summary>
public sealed record AnswerConsentCommand(Guid AnnouncementId, Guid? StudentId, string? Answer) : IRequest<ConsentChildView>, ILoggableRequest;
