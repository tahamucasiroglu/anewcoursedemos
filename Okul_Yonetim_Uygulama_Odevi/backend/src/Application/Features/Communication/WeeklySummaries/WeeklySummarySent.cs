namespace OkulYonetim.Application.Features.Communication.WeeklySummaries;

/// <summary>Özet gönderiminin sonucu (spec 0014): onaylanan taslak, bildirim giden veli sayısı ve gönderim zamanı.</summary>
public sealed record WeeklySummarySent(Guid DraftId, int NotifiedGuardians, DateTimeOffset SentAt);
