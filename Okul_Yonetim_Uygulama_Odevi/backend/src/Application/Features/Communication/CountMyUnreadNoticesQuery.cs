using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>Hoş geldin ekranının okunmamış bildirim sayısı (spec 0010 gereksinim 8): veli değilse ya da okul seçilmemişse 0.</summary>
public sealed record CountMyUnreadNoticesQuery : IRequest<int>;
