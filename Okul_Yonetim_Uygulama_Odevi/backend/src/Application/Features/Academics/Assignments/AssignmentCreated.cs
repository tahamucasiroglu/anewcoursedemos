namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>Verilen ödev (spec 0013 gereksinim 1–2): kimlik, ödevin öğrenci sayısı ve bildirim giden veli sayısı.</summary>
public sealed record AssignmentCreated(Guid Id, int StudentCount, int NotifiedGuardians);
