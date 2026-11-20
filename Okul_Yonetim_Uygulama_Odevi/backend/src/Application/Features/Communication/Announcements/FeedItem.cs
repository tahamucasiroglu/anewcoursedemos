namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Akıştaki satır: tür (<c>Announcement</c> ya da <c>SchoolNotice</c> — diğer modüllerin "Okul bildirimi"), başlık (okul bildiriminde bildirim metni), yayınlayan (okul bildiriminde boş = okul), ilgili çocuklar (sınıf/öğrenci kapsamı ve okul bildirimi), zaman, okundu mu ve onay formu mu.</summary>
public sealed record FeedItem(string Kind, Guid Id, string Title, string? AuthorName, IReadOnlyList<string> ChildNames, DateTimeOffset At, bool IsRead, bool RequiresConsent);
