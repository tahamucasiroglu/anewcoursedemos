namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Akışın bir sayfası: satırlar, sayfa numarası, sonraki sayfa var mı ve okunmamış duyuru sayısı.</summary>
public sealed record FeedPage(IReadOnlyList<FeedItem> Items, int Page, bool HasMore, int UnreadCount);
