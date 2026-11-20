namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Kurucu/öğretmen duyuru listesindeki satır: başlık, kapsam, yayınlayan, zaman, onay formu mu (son gün) ve okundu sayısı (X / Y).</summary>
public sealed record StaffAnnouncementRow(Guid Id, string Title, string Scope, string AuthorName, DateTimeOffset PublishedAt, bool RequiresConsent, DateOnly? ConsentDeadline, int RecipientCount, int ReadCount);
