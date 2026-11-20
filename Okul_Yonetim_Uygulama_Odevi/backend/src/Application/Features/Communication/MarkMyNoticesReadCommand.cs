using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Velinin ekranda gösterilen bildirimlerini okundu yapar (liste açılınca; spec 0010 gereksinim 8). Yalnızca
/// <paramref name="NoticeIds"/> içindekiler ve son değişikliği <paramref name="UpTo"/>'dan (listedeki en yeni <c>at</c>) sonra
/// olmayanlar; okunmamış sayısıyla aynı bağ süzgecinden geçer (kendi ve bağlı çocuklarının). Liste ile istek arasında gelen
/// ya da düzeltilen bildirim okunmamış kalır (0010 review F-3). Okundu yapılan sayıyı döndürür. Yalnızca kimlik ve zaman
/// taşıdığı için loglanabilir.
/// </summary>
public sealed record MarkMyNoticesReadCommand(IReadOnlyList<Guid>? NoticeIds, DateTimeOffset? UpTo) : IRequest<int>, ILoggableRequest;
