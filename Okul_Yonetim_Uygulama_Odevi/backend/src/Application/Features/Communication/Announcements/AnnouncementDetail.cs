namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Duyuru ayrıntısı: başlık, metin, kapsam, yayınlayan, zaman, onay sorusu ve son gün, onay açık mı (son gün 23:59'a kadar, Türkiye saati) ve — velide — kapsamdaki çocuklarının onay durumu.</summary>
public sealed record AnnouncementDetail(Guid Id, string Title, string Body, string Scope, string AuthorName, DateTimeOffset PublishedAt, string? ConsentQuestion, DateOnly? ConsentDeadline, bool ConsentOpen, IReadOnlyList<ConsentChildView> Children);
