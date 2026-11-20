using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>Velinin bildirimleri: yalnızca kendi bağlı çocuklarınınkiler, yeniden eskiye, en fazla 50 (spec 0010 gereksinim 8); spec 0011 akışı sayfası kadarını ister.</summary>
public sealed record ListMyNoticesQuery(int Max = CommunicationHandlers.MaxListed) : IRequest<IReadOnlyList<NoticeView>>, ILoggableRequest;
