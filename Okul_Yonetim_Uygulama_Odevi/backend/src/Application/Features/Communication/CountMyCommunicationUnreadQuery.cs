using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>Okunmamış duyuru (veli) ve mesaj (öğretmen/veli) sayısı — <c>/api/me</c> için; okul seçilmemişse ya da rol yoksa 0.</summary>
public sealed record CountMyCommunicationUnreadQuery : IRequest<CommunicationUnread>;
