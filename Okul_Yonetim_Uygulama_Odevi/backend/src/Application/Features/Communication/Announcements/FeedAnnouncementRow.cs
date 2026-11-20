namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Velinin akışındaki duyuru (depo satırı): başlık, yayınlayan, kapsam, zaman, okundu mu ve onay formu mu.</summary>
public sealed record FeedAnnouncementRow(Guid Id, string Title, string AuthorName, string Scope, DateTimeOffset PublishedAt, bool IsRead, bool RequiresConsent);
