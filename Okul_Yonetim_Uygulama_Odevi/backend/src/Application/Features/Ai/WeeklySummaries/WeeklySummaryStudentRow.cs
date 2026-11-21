namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>Sınıf listesinde öğrenci ve o haftaki özet durumu (spec 0014 gereksinim 1): <c>None</c> ("Özet yok"), <c>Draft</c> ("Taslak hazır"), <c>Sent</c> ("Gönderildi").</summary>
public sealed record WeeklySummaryStudentRow(Guid StudentId, string FullName, string Status);
