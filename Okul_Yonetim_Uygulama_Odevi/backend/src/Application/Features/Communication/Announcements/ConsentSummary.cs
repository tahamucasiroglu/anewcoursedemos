namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Onay formu sonucu: onay, ret ve cevapsız öğrenci sayıları ve öğrenci listesi.</summary>
public sealed record ConsentSummary(int Approved, int Declined, int Pending, IReadOnlyList<ConsentChildView> Students);
