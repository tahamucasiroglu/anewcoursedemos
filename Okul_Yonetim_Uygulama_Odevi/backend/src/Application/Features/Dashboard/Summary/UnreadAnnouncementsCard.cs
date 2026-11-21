using OkulYonetim.Application.Features.Communication.Announcements;

namespace OkulYonetim.Application.Features.Dashboard.Summary;

/// <summary>
/// Okunmayan duyurular kartı (spec 0017 gereksinim 5): pencerenin ilk günü (bugün dahil son 7 gün, Türkiye), okumayanı olan duyuru sayısı,
/// bu duyurulardan en az birini okumamış veli sayısı ve en çok okunmayandan ilk 10 duyuru ("X / Y okudu").
/// </summary>
public sealed record UnreadAnnouncementsCard(DateOnly Since, int AnnouncementCount, int UnreadGuardianCount, IReadOnlyList<UnreadAnnouncementRow> Top);
