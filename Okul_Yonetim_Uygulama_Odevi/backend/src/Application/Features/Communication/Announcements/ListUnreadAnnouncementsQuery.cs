using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>
/// Yayın anı <c>Since</c> ya da sonrası olan ve en az bir alıcı velinin okumadığı duyurular (spec 0017 gereksinim 5); <c>BranchId</c>
/// verilirse okul geneli duyurular ile o şubenin şube/sınıf/öğrenci kapsamlı duyuruları. Uç yoktur ve erişim denetlemez — çağıran denetler
/// (yönetici paneli kapısı; yalnızca kurucu, 0011'de sonuçları görebilen rol).
/// </summary>
public sealed record ListUnreadAnnouncementsQuery(DateTimeOffset Since, Guid? BranchId) : IRequest<UnreadAnnouncements>;
