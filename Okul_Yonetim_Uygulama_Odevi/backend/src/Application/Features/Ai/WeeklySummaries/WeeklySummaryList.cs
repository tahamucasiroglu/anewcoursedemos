namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>Seçilen sınıf ve haftanın öğrenci listesi (spec 0014 gereksinim 1): sınıf, hafta (Pazartesi, Pazar, "16–22 Kasım") ve Türkçe ad sırasıyla öğrenciler.</summary>
public sealed record WeeklySummaryList(Guid ClassroomId, string ClassroomName, DateOnly WeekStart, DateOnly WeekEnd, string WeekLabel, IReadOnlyList<WeeklySummaryStudentRow> Students);
