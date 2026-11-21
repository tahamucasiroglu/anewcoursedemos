using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Duyurunun okunma satırı (spec 0017): başlık, kapsam ve kapsam kimliği, yayınlayan, yayın anı, alıcı ve okuyan veli sayısı (0011 sonuç ekranıyla aynı sayım).</summary>
public sealed record AnnouncementReadRow(Guid Id, string Title, AnnouncementScope Scope, Guid? ScopeId, string AuthorName, DateTimeOffset PublishedAt, int RecipientCount, int ReadCount);
