using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Velinin tek bildirimi (spec 0014 gereksinim 7, AC-10 — haftalık özet ayrıntısı): yalnızca seçili okulda veli rolündeki kişi (değilse
/// 403) ve yalnızca kendisine gelmiş, <b>bağlı</b> çocuğuna ait bildirim (değilse 404 — başka velinin çocuğununki sızdırılmaz). Açılış
/// bildirimi okundu yapar. Yalnızca kimlik taşır, loglanabilir.
/// </summary>
public sealed record GetMyNoticeQuery(Guid Id) : IRequest<NoticeView>, ILoggableRequest;
