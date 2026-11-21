using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>
/// Haftalık özet onay kapısı, 1. adım (spec 0014 gereksinim 5, BR-5): Communication'ın gönderim komutu öğrenci-hafta sırası içinde
/// çağırır. Erişim <see cref="WeeklySummaryGuard"/>'la (rol yoksa 403, öğrenci başka sınıfta/okulda 404); bekleyen taslak yoksa 404,
/// gönderilmişse "Bu hafta için özet zaten gönderildi.". Uç yoktur.
/// </summary>
public sealed record GetWeeklySummaryForSendQuery(Guid StudentId, DateOnly WeekStart) : IRequest<WeeklySummaryForSend>;
