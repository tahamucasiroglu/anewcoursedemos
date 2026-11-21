namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>Haftalık özette öğretmenin sınıfı (spec 0014 gereksinim 1): kimlik, ad ve şube.</summary>
public sealed record WeeklySummaryClassroom(Guid Id, string Name, string BranchName);
