using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Duyuru sonuçları (spec 0011 gereksinim 3, 4): yalnızca yayınlayan ve kurucu; başka öğretmen 404, öğretmen/kurucu olmayan 403.</summary>
public sealed record GetAnnouncementResultsQuery(Guid Id) : IRequest<AnnouncementResults>, ILoggableRequest;
