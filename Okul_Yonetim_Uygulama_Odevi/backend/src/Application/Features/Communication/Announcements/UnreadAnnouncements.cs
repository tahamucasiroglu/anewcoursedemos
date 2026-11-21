namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Okunmayan duyurular (spec 0017 gereksinim 5): en çok okunmayandan başlayarak bütün satırlar ve bu duyuruların en az birini okumamış farklı veli sayısı.</summary>
public sealed record UnreadAnnouncements(IReadOnlyList<UnreadAnnouncementRow> Rows, int UnreadGuardianCount);
