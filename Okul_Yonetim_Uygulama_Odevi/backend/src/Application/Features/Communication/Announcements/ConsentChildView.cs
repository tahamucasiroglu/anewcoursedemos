namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Bir öğrencinin onay durumu: öğrenci, cevap (<c>Approved</c>/<c>Declined</c>; cevapsızsa boş), cevaplayan velinin adı ve zamanı.</summary>
public sealed record ConsentChildView(Guid StudentId, string StudentName, string? Answer, string? AnsweredByName, DateTimeOffset? AnsweredAt);
