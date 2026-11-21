using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>
/// Haftalık özet onay kapısı, 1. adım (spec 0014 gereksinim 5, BR-5): Communication'ın gönderim komutu öğrenci-hafta sırası içinde
/// çağırır. Erişim <see cref="WeeklySummaryGuard"/>'la (rol yoksa 403, öğrenci başka sınıfta/okulda 404); bekleyen taslak yoksa 404,
/// gönderilmişse "Bu hafta için özet zaten gönderildi.". Gönderim öğretmenin gördüğü taslağa bağlıdır (review R1-2): bekleyen taslağın
/// kimliği <c>DraftId</c> değilse ya da <c>DraftGeneratedAt</c>'ten sonra yeniden oluşturulmuşsa — ya da o haftada taslak yok ama
/// <c>DraftId</c> bu öğrencinin başka haftaya ait özetiyse (hafta döndü) — "Taslak değişti; paneli yenileyin.". Uç yoktur.
/// </summary>
public sealed record GetWeeklySummaryForSendQuery(Guid StudentId, DateOnly WeekStart, Guid DraftId, DateTimeOffset DraftGeneratedAt) : IRequest<WeeklySummaryForSend>;
