namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>Gönderime hazır bekleyen özet (spec 0014 gereksinim 5; Communication'ın gönderim komutu okur): okul, onaylayacak öğretmen, taslak, öğrenci, haftanın Pazartesi'si ve veliye gidecek dayanak satırı.</summary>
public sealed record WeeklySummaryForSend(Guid TenantId, Guid TeacherUserId, Guid DraftId, Guid StudentId, DateOnly WeekStart, string BasisLine);
