using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>Velinin bildirimleri: yalnızca kendi bağlı çocuklarınınkiler, yeniden eskiye, en fazla 50 (spec 0010 gereksinim 8).</summary>
public sealed record ListMyNoticesQuery : IRequest<IReadOnlyList<NoticeView>>, ILoggableRequest;
