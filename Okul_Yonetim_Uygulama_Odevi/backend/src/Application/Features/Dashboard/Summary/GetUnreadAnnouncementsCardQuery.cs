using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Dashboard.Summary;

/// <summary>Günlük özetin okunmayan duyurular kartı (spec 0017 gereksinim 5); <c>BranchId</c> boşsa bütün şubeler. Yalnızca kurucu.</summary>
public sealed record GetUnreadAnnouncementsCardQuery(Guid? BranchId) : IRequest<UnreadAnnouncementsCard>;
