using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Velinin duyuru akışı (spec 0011 gereksinim 2, 7): alıcısı olduğu duyurular ve bağlı çocuklarının okul bildirimleri, yeniden eskiye, sayfa başına 20 (sayfa 1'den başlar).</summary>
public sealed record ListMyFeedQuery(int Page = 1) : IRequest<FeedPage>, ILoggableRequest;
