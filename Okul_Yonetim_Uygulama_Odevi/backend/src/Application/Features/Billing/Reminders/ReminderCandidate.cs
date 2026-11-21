using OkulYonetim.Domain.Billing;

namespace OkulYonetim.Application.Features.Billing.Reminders;

/// <summary>Hatırlatma adayı (spec 0015 gereksinim 9): geçerli planın vadesi geçmiş ve kalanı olan taksidi, öğrencisi ve gönderilmiş en büyük hatırlatma sırası (yoksa 0).</summary>
public sealed record ReminderCandidate(Installment Installment, Guid StudentId, int LastSentSequence);
