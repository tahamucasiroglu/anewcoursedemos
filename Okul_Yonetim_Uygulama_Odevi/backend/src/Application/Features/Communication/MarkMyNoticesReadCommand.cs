using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Velinin bildirimlerini okundu yapar (liste açılınca; spec 0010 gereksinim 8): okunmamış sayısıyla aynı bağ süzgecinden
/// geçen (kendi ve bağlı çocuklarının) <b>bütün</b> okunmamış bildirimlerden son değişikliği <paramref name="UpTo"/>'dan
/// (listedeki en yeni <c>at</c>; UTC'ye çevrilir) sonra olmayanlar — listede gösterilmeyen eskiler de (liste 50 ile sınırlı;
/// 0010 review 2, bulgu 1). Liste ile istek arasında gelen ya da düzeltilen bildirim okunmamış kalır (0010 review F-3).
/// Okundu yapılan sayıyı döndürür. Yalnızca zaman taşıdığı için loglanabilir.
/// </summary>
public sealed record MarkMyNoticesReadCommand(DateTimeOffset? UpTo) : IRequest<int>, ILoggableRequest;
