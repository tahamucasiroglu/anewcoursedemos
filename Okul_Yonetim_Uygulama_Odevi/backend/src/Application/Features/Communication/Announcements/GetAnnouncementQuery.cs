using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Duyuru ayrıntısı (spec 0011 gereksinim 3, 4): alıcı veli açınca ilk okuma kaydedilir ve kapsamdaki her çocuğunun onay durumu döner; yayınlayan ve kurucu da açabilir; başkası 404.</summary>
public sealed record GetAnnouncementQuery(Guid Id) : IRequest<AnnouncementDetail>, ILoggableRequest;
