namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>En az bir alıcı velinin okumadığı duyuru (spec 0017 kartı): başlık, yayın anı, yayınlayan, kapsam ve "X / Y okudu" sayıları (metin yok).</summary>
public sealed record UnreadAnnouncementRow(Guid Id, string Title, DateTimeOffset PublishedAt, string AuthorName, string Scope, int RecipientCount, int ReadCount);
