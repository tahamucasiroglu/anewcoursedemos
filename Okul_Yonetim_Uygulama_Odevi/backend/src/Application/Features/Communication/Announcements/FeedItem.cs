namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Akıştaki satır: tür (<c>Announcement</c>, <c>SchoolNotice</c> — diğer modüllerin "Okul bildirimi" — ya da spec 0014 <c>WeeklySummary</c> — haftalık gelişim özeti, ayrıntısı <c>api/notices/{id}</c>), başlık (okul bildiriminde bildirim metni, haftalık özette "Haftalık gelişim özeti — 16–22 Kasım"), yayınlayan (okul bildiriminde boş = okul), ilgili çocuklar (sınıf/öğrenci kapsamı ve okul bildirimi), zaman, okundu mu ve onay formu mu.</summary>
public sealed record FeedItem(string Kind, Guid Id, string Title, string? AuthorName, IReadOnlyList<string> ChildNames, DateTimeOffset At, bool IsRead, bool RequiresConsent);
