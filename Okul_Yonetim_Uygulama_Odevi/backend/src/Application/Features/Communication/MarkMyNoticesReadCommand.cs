using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>Velinin okunmamış bütün bildirimlerini okundu yapar (liste açılınca; spec 0010 gereksinim 8). Okundu yapılan sayıyı döndürür.</summary>
public sealed record MarkMyNoticesReadCommand : IRequest<int>, ILoggableRequest;
